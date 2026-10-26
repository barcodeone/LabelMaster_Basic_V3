using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BarcodePrint.Model;

namespace BarcodePrint.WpfView
{
    public partial class LabelSetupWizardDialog : Window
    {
        private int _currentStep = 1;
        private readonly List<(double Width, double Height)> _customSizes = new List<(double Width, double Height)>();

        public double SelectedWidth { get; private set; } = 80.0;
        public double SelectedHeight { get; private set; } = 60.0;
        public string SelectedPaperType { get; private set; } = "GAP";
        public int SelectedRotation { get; private set; } = 0;
        public bool SelectedUseRibbon { get; private set; } = false;

        public LabelSetupWizardDialog(double initialWidth = 0, double initialHeight = 0)
        {
            InitializeComponent();
            LoadCustomSizes();

            // 마지막으로 사용했던 라벨 규격 불러오기 (파라미터 우선, 없으면 AppSettings에서 로드)
            double initW = initialWidth;
            double initH = initialHeight;

            // 구버전 기본값(70x28)이거나 유효하지 않은 경우 AppSettings 로드 시도
            if ((Math.Abs(initW - 70.0) < 0.1 && Math.Abs(initH - 28.0) < 0.1) || initW <= 0 || initH <= 0)
            {
                try
                {
                    var settings = new AppSettings();
                    settings.Load();
                    if (settings.PaperWidth > 0 && settings.PaperHeight > 0)
                    {
                        initW = settings.PaperWidth;
                        initH = settings.PaperHeight;
                    }
                }
                catch { }
            }

            // 여전히 구버전 70x28이거나 유효하지 않으면 신규 표준 80x60 적용
            if ((Math.Abs(initW - 70.0) < 0.1 && Math.Abs(initH - 28.0) < 0.1) || initW <= 0 || initH <= 0)
            {
                initW = 80.0;
                initH = 60.0;
            }

            SelectedWidth = initW;
            SelectedHeight = initH;

            if (TxtWidth != null) TxtWidth.Text = initW.ToString("0.#", CultureInfo.InvariantCulture);
            if (TxtHeight != null) TxtHeight.Text = initH.ToString("0.#", CultureInfo.InvariantCulture);

            UpdateStepUi();
        }



        #region Custom Sizes Load / Save / Render

        private void LoadCustomSizes()
        {
            try
            {
                _customSizes.Clear();
                var settings = new AppSettings();
                settings.Load();

                string raw = settings.CustomPaperSizes ?? "";
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    string[] items = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string item in items)
                    {
                        string[] parts = item.Split(',');
                        if (parts.Length == 2 &&
                            double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double w) &&
                            double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double h))
                        {
                            if (w > 0 && h > 0)
                            {
                                _customSizes.Add((w, h));
                            }
                        }
                    }
                }
            }
            catch { }

            RenderCustomPresetChips();
        }

        private void SaveCustomSizes()
        {
            try
            {
                var settings = new AppSettings();
                settings.Load();

                var strList = _customSizes.Select(s => $"{s.Width.ToString("0.#", CultureInfo.InvariantCulture)},{s.Height.ToString("0.#", CultureInfo.InvariantCulture)}");
                settings.CustomPaperSizes = string.Join(";", strList);
                settings.Save();
            }
            catch { }
        }

        private void RenderCustomPresetChips()
        {
            PanelCustomPresets.Children.Clear();

            if (_customSizes.Count == 0)
            {
                TxtNoCustomPresets.Visibility = Visibility.Visible;
                return;
            }

            TxtNoCustomPresets.Visibility = Visibility.Collapsed;

            foreach (var size in _customSizes)
            {
                double w = size.Width;
                double h = size.Height;

                // 칩 컨테이너
                var chipBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                    BorderThickness = new Thickness(1.2),
                    CornerRadius = new CornerRadius(18),
                    Padding = new Thickness(12, 5, 8, 5),
                    Margin = new Thickness(0, 0, 8, 8),
                    Cursor = Cursors.Hand
                };

                var stack = new StackPanel { Orientation = Orientation.Horizontal };

                // 규격 텍스트 (클릭 시 입력창에 반영)
                var txt = new TextBlock
                {
                    Text = $"⭐ {w:0.#} x {h:0.#} mm",
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                    VerticalAlignment = VerticalAlignment.Center
                };
                txt.MouseLeftButtonDown += (s, e) =>
                {
                    TxtWidth.Text = w.ToString("0.#", CultureInfo.InvariantCulture);
                    TxtHeight.Text = h.ToString("0.#", CultureInfo.InvariantCulture);
                };
                stack.Children.Add(txt);

                // 마우스 호버 효과
                chipBorder.MouseEnter += (s, e) =>
                {
                    chipBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                    chipBorder.Background = new SolidColorBrush(Color.FromRgb(239, 246, 255));
                };
                chipBorder.MouseLeave += (s, e) =>
                {
                    chipBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225));
                    chipBorder.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                };

                // 삭제 [✕] 버튼
                var btnDel = new Button
                {
                    Content = "✕",
                    FontSize = 10.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Margin = new Thickness(6, 0, 0, 0),
                    Padding = new Thickness(4, 1, 4, 1),
                    Cursor = Cursors.Hand,
                    ToolTip = $"맞춤 규격 ({w:0.#} x {h:0.#} mm) 삭제"
                };
                btnDel.MouseEnter += (s, e) => btnDel.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                btnDel.MouseLeave += (s, e) => btnDel.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
                btnDel.Click += (s, e) =>
                {
                    e.Handled = true;
                    RemoveCustomSize(w, h);
                };
                stack.Children.Add(btnDel);

                chipBorder.Child = stack;
                PanelCustomPresets.Children.Add(chipBorder);
            }
        }

        private void OnSaveCustomSizeClick(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(TxtWidth.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double w) || w <= 0 ||
                !double.TryParse(TxtHeight.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double h) || h <= 0)
            {
                MessageBox.Show("올바른 가로 및 세로 크기(mm)를 입력한 후 저장 버튼을 눌러주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 소수점 1자리 반올림
            w = Math.Round(w, 1);
            h = Math.Round(h, 1);

            // 중복 검사
            bool exists = _customSizes.Any(item => Math.Abs(item.Width - w) < 0.05 && Math.Abs(item.Height - h) < 0.05);
            if (exists)
            {
                MessageBox.Show($"이미 등록되어 있는 맞춤 규격입니다. ({w:0.#} x {h:0.#} mm)", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _customSizes.Add((w, h));
            SaveCustomSizes();
            RenderCustomPresetChips();

            MessageBox.Show($"규격 {w:0.#} x {h:0.#} mm가 '내가 저장한 맞춤 규격'에 등록되었습니다.", "저장 완료", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RemoveCustomSize(double w, double h)
        {
            var result = MessageBox.Show($"맞춤 규격 [{w:0.#} x {h:0.#} mm]을 목록에서 삭제하시겠습니까?", "규격 삭제 확인", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _customSizes.RemoveAll(item => Math.Abs(item.Width - w) < 0.05 && Math.Abs(item.Height - h) < 0.05);
                SaveCustomSizes();
                RenderCustomPresetChips();
            }
        }

        #endregion

        private void OnFinishClick(object sender, RoutedEventArgs e)
        {
            // 1단계: 라벨 규격(Width, Height) 유효성 검사 및 설정
            if (!double.TryParse(TxtWidth.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double w) || w <= 0 ||
                !double.TryParse(TxtHeight.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double h) || h <= 0)
            {
                MessageBox.Show("올바른 가로 및 세로 크기(mm)를 입력해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedWidth = w;
            SelectedHeight = h;

            // 2단계 (용지 종류): 현재 선택값 반영 (기본: GAP)
            if (RbPaperGap != null && RbPaperGap.IsChecked == true) SelectedPaperType = "GAP";
            else if (RbPaperBlackMark != null && RbPaperBlackMark.IsChecked == true) SelectedPaperType = "BLACK_MARK";
            else if (RbPaperContinuous != null && RbPaperContinuous.IsChecked == true) SelectedPaperType = "CONTINUOUS";
            else SelectedPaperType = "GAP";

            // 3단계 (인쇄 방향): 현재 선택값 반영 (기본: 0도 정방향)
            if (RbOrientLandscape != null && RbOrientLandscape.IsChecked == true) SelectedRotation = 90;
            else SelectedRotation = 0;

            // 4단계 (인쇄 방식): 현재 선택값 반영 (기본: 감열식)
            if (RbMethodThermalTransfer != null && RbMethodThermalTransfer.IsChecked == true) SelectedUseRibbon = true;
            else SelectedUseRibbon = false;

            // 즉시 완료 및 마법사 닫기
            SaveLastUsedSize(SelectedWidth, SelectedHeight);
            this.DialogResult = true;
            this.Close();
        }

        private void SaveLastUsedSize(double w, double h)
        {
            try
            {
                var settings = new AppSettings();
                settings.Load();
                settings.PaperWidth = (int)Math.Round(w);
                settings.PaperHeight = (int)Math.Round(h);
                settings.Save();
            }
            catch { }
        }

        private void OnPrevClick(object sender, RoutedEventArgs e)
        {
            if (_currentStep > 1)
            {
                _currentStep--;
                UpdateStepUi();
            }
        }

        private void OnNextClick(object sender, RoutedEventArgs e)
        {
            if (_currentStep == 1)
            {
                // 크기 검증
                if (!double.TryParse(TxtWidth.Text.Trim(), out double w) || w <= 0 ||
                    !double.TryParse(TxtHeight.Text.Trim(), out double h) || h <= 0)
                {
                    MessageBox.Show("올바른 가로 및 세로 크기(mm)를 입력해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SelectedWidth = w;
                SelectedHeight = h;
            }
            else if (_currentStep == 2)
            {
                if (RbPaperGap.IsChecked == true) SelectedPaperType = "GAP";
                else if (RbPaperBlackMark.IsChecked == true) SelectedPaperType = "BLACK_MARK";
                else if (RbPaperContinuous.IsChecked == true) SelectedPaperType = "CONTINUOUS";
            }
            else if (_currentStep == 3)
            {
                SelectedRotation = (RbOrientPortrait.IsChecked == true) ? 0 : 90;
            }
            else if (_currentStep == 4)
            {
                SelectedUseRibbon = (RbMethodThermalTransfer.IsChecked == true);

                // 마법사 완료!
                SaveLastUsedSize(SelectedWidth, SelectedHeight);
                this.DialogResult = true;
                this.Close();
                return;
            }

            _currentStep++;
            UpdateStepUi();
        }

        private void OnWidthMinusClick(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(TxtWidth.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double w))
            {
                w = Math.Max(10, w - (Keyboard.Modifiers == ModifierKeys.Shift ? 10 : 5));
                TxtWidth.Text = w.ToString("0.#", CultureInfo.InvariantCulture);
            }
        }

        private void OnWidthPlusClick(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(TxtWidth.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double w))
            {
                w = Math.Min(300, w + (Keyboard.Modifiers == ModifierKeys.Shift ? 10 : 5));
                TxtWidth.Text = w.ToString("0.#", CultureInfo.InvariantCulture);
            }
        }

        private void OnHeightMinusClick(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(TxtHeight.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double h))
            {
                h = Math.Max(10, h - (Keyboard.Modifiers == ModifierKeys.Shift ? 10 : 5));
                TxtHeight.Text = h.ToString("0.#", CultureInfo.InvariantCulture);
            }
        }

        private void OnHeightPlusClick(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(TxtHeight.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double h))
            {
                h = Math.Min(300, h + (Keyboard.Modifiers == ModifierKeys.Shift ? 10 : 5));
                TxtHeight.Text = h.ToString("0.#", CultureInfo.InvariantCulture);
            }
        }

        private void OnDimensionsChanged(object sender, TextChangedEventArgs e)
        {
            if (BorderMiniaturePreview == null || TxtMiniatureDims == null || TxtMiniatureRatioDesc == null) return;

            if (double.TryParse(TxtWidth?.Text?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double w) && w > 0 &&
                double.TryParse(TxtHeight?.Text?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double h) && h > 0)
            {
                TxtMiniatureDims.Text = $"{w:0.#}×{h:0.#}";

                // 박스 크기 계산 (최대 110 x 70 안에서 fit)
                double maxBoxW = 110.0;
                double maxBoxH = 70.0;
                double scale = Math.Min(maxBoxW / w, maxBoxH / h);
                double previewW = Math.Max(24, Math.Min(maxBoxW, w * scale));
                double previewH = Math.Max(24, Math.Min(maxBoxH, h * scale));

                BorderMiniaturePreview.Width = previewW;
                BorderMiniaturePreview.Height = previewH;

                if (Math.Abs(w - h) < 2)
                {
                    TxtMiniatureRatioDesc.Text = "1:1 정사각형 라벨";
                }
                else if (w > h)
                {
                    TxtMiniatureRatioDesc.Text = $"가로형 라벨 ({(w / h):0.#}:1)";
                }
                else
                {
                    TxtMiniatureRatioDesc.Text = $"세로형 라벨 (1:{(h / w):0.#})";
                }
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void UpdateStepUi()
        {
            // 1. 헤더 텍스트 및 배지 변경
            TxtStepBadge.Text = $"{_currentStep}단계 / 4";
            if (ProgressStepBar != null)
            {
                ProgressStepBar.Value = _currentStep;
            }
            BtnPrev.Visibility = (_currentStep > 1) ? Visibility.Visible : Visibility.Collapsed;
            
            // 텍스트 길이 축소: "완료 및 편집 시작 ✔" -> "편집 시작 ▶"
            BtnNext.Content = (_currentStep == 4) ? "편집 시작 ▶" : "다음 단계 ▶";
            if (BtnQuickFinish != null)
            {
                BtnQuickFinish.Visibility = (_currentStep < 4) ? Visibility.Visible : Visibility.Collapsed;
            }

            // 2. 인디케이터 점 색상
            DotStep1.Fill = new SolidColorBrush(_currentStep >= 1 ? Color.FromRgb(37, 99, 235) : Color.FromRgb(203, 213, 225));
            DotStep2.Fill = new SolidColorBrush(_currentStep >= 2 ? Color.FromRgb(37, 99, 235) : Color.FromRgb(203, 213, 225));
            DotStep3.Fill = new SolidColorBrush(_currentStep >= 3 ? Color.FromRgb(37, 99, 235) : Color.FromRgb(203, 213, 225));
            DotStep4.Fill = new SolidColorBrush(_currentStep >= 4 ? Color.FromRgb(37, 99, 235) : Color.FromRgb(203, 213, 225));

            // 3. 레이아웃 전환
            LayoutStep1.Visibility = (_currentStep == 1) ? Visibility.Visible : Visibility.Collapsed;
            LayoutStep2.Visibility = (_currentStep == 2) ? Visibility.Visible : Visibility.Collapsed;
            LayoutStep3.Visibility = (_currentStep == 3) ? Visibility.Visible : Visibility.Collapsed;
            LayoutStep4.Visibility = (_currentStep == 4) ? Visibility.Visible : Visibility.Collapsed;

            switch (_currentStep)
            {
                case 1:
                    TxtStepTitle.Text = "라벨 규격(사이즈) 선택";
                    TxtStepDesc.Text = "출력할 라벨의 가로 및 세로 크기(mm)를 입력하세요.";
                    break;
                case 2:
                    TxtStepTitle.Text = "용지 종류 선택";
                    TxtStepDesc.Text = "프린터에 장착된 라벨 용지의 센서 감지 방식을 선택하세요.";
                    break;
                case 3:
                    TxtStepTitle.Text = "인쇄 방향 선택";
                    TxtStepDesc.Text = "라벨의 기본 회전각 및 인쇄 출력 방향을 선택하세요.";
                    break;
                case 4:
                    TxtStepTitle.Text = "인쇄 방식 및 리본 유무";
                    TxtStepDesc.Text = "프린터 인쇄 헤드의 가열 방식(감열식 vs 잉크 리본 열전사)을 선택하세요.";
                    break;
            }
        }
    }
}
