using MyPaint.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyPaint
{
    public class HistoryManager
    {
        private List<byte[]> _ctrlzList = new List<byte[]>();
        private List<byte[]> _ctrlyList = new List<byte[]>();

        public bool CanCtrlZ => _ctrlzList.Count > 0;
        public bool CanCtrlY => _ctrlyList.Count > 0;

        //чтоб активный слой не дублировался
        private readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            IncludeFields = true,
            ReferenceHandler = ReferenceHandler.Preserve
        };

        public void SaveState(DrawingProject project)
        {
            if (project == null) return;

            byte[] snapshot = JsonSerializer.SerializeToUtf8Bytes(project, _options);
            _ctrlzList.Add(snapshot);
            _ctrlyList.Clear();

            if (_ctrlzList.Count > 50) _ctrlzList.RemoveAt(0);
        }

        public DrawingProject CtrlZ(DrawingProject currentProject)
        {
            if (_ctrlzList.Count == 0) return currentProject;

            byte[] currentSnapshot = JsonSerializer.SerializeToUtf8Bytes(currentProject, _options);
            _ctrlyList.Add(currentSnapshot);

            byte[] lastState = _ctrlzList.Last();
            _ctrlzList.RemoveAt(_ctrlzList.Count - 1);

            return JsonSerializer.Deserialize<DrawingProject>(lastState, _options);
        }

        public DrawingProject CtrlY(DrawingProject currentProject)
        {
            if (_ctrlyList.Count == 0) return currentProject;

            byte[] currentSnapshot = JsonSerializer.SerializeToUtf8Bytes(currentProject, _options);
            _ctrlzList.Add(currentSnapshot);

            byte[] nextState = _ctrlyList.Last();
            _ctrlyList.RemoveAt(_ctrlyList.Count - 1);

            return JsonSerializer.Deserialize<DrawingProject>(nextState, _options);
        }
    }
}