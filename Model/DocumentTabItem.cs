using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BarcodePrint.Model
{
    /// <summary>
    /// 다중 문서 탭 편집을 위한 개별 문서 탭 상태 모델
    /// </summary>
    public class DocumentTabItem : INotifyPropertyChanged
    {
        public string Id { get; } = Guid.NewGuid().ToString();

        private string _title = "새 문서";
        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _sizeBadge = "";
        public string SizeBadge
        {
            get => _sizeBadge;
            set
            {
                if (_sizeBadge != value)
                {
                    _sizeBadge = value;
                    OnPropertyChanged();
                }
            }
        }

        public string FilePath { get; set; } = "";
        public LabelTemplateModel Template { get; set; }
        public List<List<TemplateElement>> Pages { get; set; } = new List<List<TemplateElement>>();
        public int CurrentPageIndex { get; set; } = 0;

        public int PaperWidth { get; set; } = 80;
        public int PaperHeight { get; set; } = 60;
        public int LabelRotationAngle { get; set; } = 0;
        public string PaperType { get; set; } = "GAP";
        public int GapHeight { get; set; } = 2;

        public int ActiveColumnCount { get; set; } = 1;
        public string[] ColumnInputs { get; set; } = new string[8];

        public Stack<List<TemplateElement>> UndoStack { get; set; } = new Stack<List<TemplateElement>>();
        public Stack<List<TemplateElement>> RedoStack { get; set; } = new Stack<List<TemplateElement>>();
        public string SelectedElementId { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
