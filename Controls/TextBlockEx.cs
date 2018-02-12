using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Markup;
using System.Globalization;
using System.Windows.Controls.Primitives;

namespace MCUTerm.Controls
{
    public partial class TextBlockEx : Control
    {
        protected class GlyphHelper
        {
            protected GlyphTypeface glyphTypeface;
            protected IDictionary<int, ushort> glyphIndexMap;
            protected Dictionary<ushort, double> glyphAdvanceWidthMap = new Dictionary<ushort, double>();

            protected static MethodInfo glyphTryCreate;

            protected double width;
            protected double height;
            protected double baseline;
            protected double fontSize;
            protected double dpiScaleX;

            public double Baseline => baseline;
            public double Width => width;
            public double Height => height;

            public GlyphHelper(FontFamily fontFamily, FontStyle style, FontWeight weight, FontStretch stretch, double fontSize, double dpiScaleX)
            {
                this.fontSize = fontSize;
                this.dpiScaleX = dpiScaleX;

                Typeface typeface = new Typeface(fontFamily, style, weight, stretch);
                if (!typeface.TryGetGlyphTypeface(out glyphTypeface) ||
                    !(new System.Drawing.FontFamily(fontFamily.Source).IsStyleAvailable(System.Drawing.FontStyle.Regular)))
                {
                    throw new Exception("Invalid Font Selected.");
                }

                glyphIndexMap = glyphTypeface.CharacterToGlyphMap;

                width = Math.Round(glyphTypeface.AdvanceWidths['w'] * fontSize * dpiScaleX, MidpointRounding.ToEven) / dpiScaleX;
                height = Math.Round(glyphTypeface.Height * fontSize * dpiScaleX, MidpointRounding.ToEven) / dpiScaleX;
                baseline = Math.Round(glyphTypeface.Baseline * fontSize * dpiScaleX, MidpointRounding.ToEven) / dpiScaleX;

                glyphTryCreate = typeof(GlyphRun).GetMethod("TryCreate", BindingFlags.Static | BindingFlags.NonPublic, null,
                    new[] { typeof(GlyphTypeface), typeof(Int32), typeof(Boolean), typeof(Double), typeof(Single), typeof(IList<ushort>),
                        typeof(Point), typeof(IList<double>), typeof(IList<Point>), typeof(IList<char>), typeof(String),
                        typeof(IList<ushort>), typeof(IList<bool>), typeof(XmlLanguage), typeof(TextFormattingMode) }, null);
            }

            public ushort GetGlyphIndex(int symbol)
            {
                return glyphIndexMap[symbol];
            }

            public GlyphRun CreateGlyphRun(Point point, IList<ushort> indices, IList<double> widths)
            {
                if (glyphTryCreate == null)
                    return new GlyphRun(glyphTypeface, 0, false, fontSize, (float)dpiScaleX, indices, point, widths, null, null, null, null, null, null);

                return (GlyphRun)glyphTryCreate.Invoke(null, new object[] { glyphTypeface, 0, false, fontSize, (float)dpiScaleX, indices, point, widths,
                    null, null, null, null, null, null, TextFormattingMode.Display });
            }
        }

        protected class Glyph
        {
            public ushort glyphIndex;
            public Brush background;
            public Brush foreground;
            public bool highlighted;
            public bool selected;
            public char symbol;
        }

        protected class Row
        {
            public List<Glyph> glyphs = new List<Glyph>();

            public int TextLength => glyphs.Count() + 1; // + '\n'

            public void Add(char symbol, ushort glyphIndex, Brush foreground, Brush background)
            {
                Glyph glyph = new Glyph
                {
                    symbol = symbol,
                    glyphIndex = glyphIndex,
                    background = background,
                    foreground = foreground,
                    selected = false,
                    highlighted = false
                };

                glyphs.Add(glyph);
            }
        }

        const int TabSize = 4;
        const int MaxWordWrapLenght = 20;

        private VisualCollection _visuals;
        private ScrollBar _hScroll;
        private ScrollBar _vScroll;

        protected int _verticalOffset;
        protected int _horizontalOffset;
        protected int _maxWidth;

        protected GlyphHelper glyphHelper;

        protected string text;
        protected List<Row> rows;

        protected string originalText;
        protected List<Row> originalRows = new List<Row>();

        public string Text => text;

        protected int selectionStart;
        protected int selectionLength;
        protected string selectedText;

        public string HighlightedText
        {
            get => (string)base.GetValue(HighlightedTextProperty);
            set => base.SetValue(HighlightedTextProperty, value);
        }

        public static readonly DependencyProperty HighlightedTextProperty =
          DependencyProperty.Register("HighlightedText", typeof(string), typeof(TextBlockEx),
                                      new FrameworkPropertyMetadata("", OnHighlightTextChanged));

        public bool DisableFilterHighlighted
        {
            get => (bool)base.GetValue(DisableFilterHighlightedProperty);
            set => base.SetValue(DisableFilterHighlightedProperty, value);
        }

        public static readonly DependencyProperty DisableFilterHighlightedProperty =
          DependencyProperty.Register("DisableFilterHighlighted", typeof(bool), typeof(TextBlockEx),
                                      new FrameworkPropertyMetadata(false, OnHighlightTextChanged));

        public bool EnableHighlight
        {
            get => (bool)base.GetValue(EnableHighlightProperty);
            set => base.SetValue(EnableHighlightProperty, value);
        }

        public static readonly DependencyProperty EnableHighlightProperty =
          DependencyProperty.Register("EnableHighlight", typeof(bool), typeof(TextBlockEx),
                                      new FrameworkPropertyMetadata(true, OnHighlightTextChanged));

        public bool MatchCaseHighlighted
        {
            get => (bool)base.GetValue(MatchCaseHighlightedProperty);
            set => base.SetValue(MatchCaseHighlightedProperty, value);
        }

        public static readonly DependencyProperty MatchCaseHighlightedProperty =
          DependencyProperty.Register("MatchCaseHighlighted", typeof(bool), typeof(TextBlockEx),
                                      new FrameworkPropertyMetadata(true, OnHighlightTextChanged));


        public Brush SelectionBackground
        {
            get => (Brush)base.GetValue(SelectionBackgroundProperty);
            set => base.SetValue(SelectionBackgroundProperty, value);
        }

        public static readonly DependencyProperty SelectionBackgroundProperty =
          DependencyProperty.Register("SelectionBackground", typeof(Brush), typeof(TextBlockEx),
                                      new FrameworkPropertyMetadata((Brush)null, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush SelectionForeground
        {
            get => (Brush)base.GetValue(SelectionForegroundProperty);
            set => base.SetValue(SelectionForegroundProperty, value);
        }

        public static readonly DependencyProperty SelectionForegroundProperty =
          DependencyProperty.Register("SelectionForeground", typeof(Brush), typeof(TextBlockEx),
                                      new FrameworkPropertyMetadata((Brush)null, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush HighlightBackground
        {
            get => (Brush)base.GetValue(HighlightBackgroundProperty);
            set => base.SetValue(HighlightBackgroundProperty, value);
        }

        public static readonly DependencyProperty HighlightBackgroundProperty =
          DependencyProperty.Register("HighlightBackground", typeof(Brush), typeof(TextBlockEx),
                                      new FrameworkPropertyMetadata((Brush)null, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush HighlightForeground
        {
            get => (Brush)base.GetValue(HighlightForegroundProperty);
            set => base.SetValue(HighlightForegroundProperty, value);
        }

        public static readonly DependencyProperty HighlightForegroundProperty =
          DependencyProperty.Register("HighlightForeground", typeof(Brush), typeof(TextBlockEx),
                                      new FrameworkPropertyMetadata((Brush)null, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool WordWrap
        {
            get { return (bool)GetValue(WordWrapProperty); }
            set { SetValue(WordWrapProperty, value); }
        }

        public static readonly DependencyProperty WordWrapProperty =
            DependencyProperty.Register("WordWrap", typeof(bool), typeof(TextBlockEx),
                                        new FrameworkPropertyMetadata(false, OnWordWrapChanged));

        public string SelectedText
        {
            get => selectedText;
        }

        public int SelectionStart
        {
            get => selectionStart;
            set => Select(value, selectionLength);
        }

        public int SelectionLength
        {
            get => selectionLength;
            set => Select(selectionStart, value);
        }

        public TextBlockEx() : base()
        {
            Focusable = true;
            rows = originalRows;
            selectedText = "";

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy,
                (sender, e) => { Clipboard.SetText(((TextBlockEx)sender).SelectedText); },
                (sender, e) => { e.CanExecute = ((TextBlockEx)sender).SelectedText.Length > 0; }
            ));

            CommandBindings.Add(new CommandBinding(ApplicationCommands.SelectAll,
                (sender, e) => { ((TextBlockEx)sender).Select(0, ((TextBlockEx)sender).Text.Length); },
                (sender, e) => { e.CanExecute = ((TextBlockEx)sender).Text.Length > 0; }
            ));

            _maxWidth = 0;
            _verticalOffset = 0;
            _horizontalOffset = 0;

            _hScroll = new ScrollBar();
            _vScroll = new ScrollBar();
            _hScroll.Orientation = Orientation.Horizontal;
            _vScroll.Orientation = Orientation.Vertical;
            _hScroll.ValueChanged += OnHScrollValueChanged;
            _vScroll.ValueChanged += OnVScrollValueChanged;

            _visuals = new VisualCollection(this);
            _visuals.Add(_hScroll);
            _visuals.Add(_vScroll);
        }

        public void ClearText()
        {
            rows.Clear();
            UpdateSelection(0, 0);
            InvalidateVisual();
        }

        public void AddText(string text)
        {
            AddText(text, null, null);
        }

        public void AddText(string text, Brush background, Brush foreground)
        {
            if (text.Length == 0)
                return;

            double dpiScaleX = VisualTreeHelper.GetDpi(this).DpiScaleX;
            if (glyphHelper == null)
                glyphHelper = new GlyphHelper(FontFamily, FontStyle, FontWeight, FontStretch, FontSize, dpiScaleX);

            if (originalRows.Count() == 0)
                originalRows.Add(new Row());

            int pos = 0;
            while (pos < text.Length)
            {
                char symbol = text[pos++];
                if (symbol == '\r' || symbol == '\n')
                {
                    originalRows.Add(new Row());
                    originalText += '\n';

                    if (pos < text.Length && symbol != text[pos] &&
                        (text[pos] == '\n' || text[pos] != '\r'))
                    {
                        pos++;
                    }

                    continue;
                }

                int repeat = 1;
                if (symbol == '\t')
                {
                    repeat = ((TabSize - originalRows.Last().glyphs.Count() % TabSize) & 0x03) + 1;
                    symbol = ' ';
                }

                while (repeat > 0)
                {
                    originalRows.Last().Add(symbol, glyphHelper.GetGlyphIndex(symbol), background, foreground);
                    originalText += symbol;
                    repeat--;
                }
            }

            UpdateContent();
        }

        public void Select(int start, int length)
        {
            if (length == 0 && SelectionLength == 0)
                return;

            UpdateSelection(start, length);
        }

        public void ScrollToBottom()
        {
            if (_vScroll.ViewportSize > 0 && _vScroll.Maximum < rows.Count())
                _vScroll.Value = Math.Max(0, rows.Count() - _vScroll.ViewportSize);
        }

        protected override int VisualChildrenCount
        {
            get => _visuals.Count;
        }

        protected override Visual GetVisualChild(int index)
        {
            return _visuals[index];
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            Point pt = hitTestParameters.HitPoint;

            if (pt.X >= 0 && pt.X < (ActualWidth - _vScroll.ActualWidth) && pt.Y >= 0 && pt.Y < (ActualHeight - _hScroll.ActualHeight))
            {
                return new PointHitTestResult(this, pt);
            }

            return base.HitTestCore(hitTestParameters);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            _hScroll.Measure(constraint);
            _vScroll.Measure(constraint);

            Size hSize = _hScroll.DesiredSize;
            Size vSize = _vScroll.DesiredSize;
            return new Size(hSize.Width + vSize.Width, hSize.Height + vSize.Height);
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            if (glyphHelper != null)
            {
                double newHViewportSize = (int)((arrangeBounds.Width - _vScroll.DesiredSize.Width) / glyphHelper.Width);
                double newVViewportSize = (int)((arrangeBounds.Height - _hScroll.DesiredSize.Height) / glyphHelper.Height);

                bool updateRange = false;
                if (newHViewportSize != _hScroll.ViewportSize || newVViewportSize != _vScroll.ViewportSize)
                    updateRange = true;

                _hScroll.ViewportSize = newHViewportSize;
                _vScroll.ViewportSize = newVViewportSize;

                if (updateRange)
                    UpdateScrollbarsRange();
            }

            _hScroll.Arrange(new Rect(0, arrangeBounds.Height - _hScroll.DesiredSize.Height, arrangeBounds.Width - _vScroll.DesiredSize.Width, _hScroll.DesiredSize.Height));
            _vScroll.Arrange(new Rect(arrangeBounds.Width - _vScroll.DesiredSize.Width, 0, _vScroll.DesiredSize.Width, arrangeBounds.Height - _hScroll.DesiredSize.Height));
            return arrangeBounds;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo info)
        {
            if (WordWrap == true && info.WidthChanged == true)
                UpdateContent();

            UpdateScrollbarsVisibility(info.NewSize.Width, info.NewSize.Height);
        }

        protected override void OnRender(DrawingContext dc)
        {
            Rect rect = new Rect(0, 0, Math.Max(0.0, RenderSize.Width - _vScroll.ActualWidth), Math.Max(0.0, RenderSize.Height - _hScroll.ActualHeight));
            dc.DrawRectangle(Background, null, rect);
            if (rows.Count() == 0)
                return;

            double y = 0;
            for (int rowIndex = _verticalOffset; rowIndex < rows.Count(); rowIndex++)
            {
                if (y + glyphHelper.Height >= rect.Height)
                    break;

                double x = 0;
                Row row = rows[rowIndex];
                for (int glyphIndex = _horizontalOffset; glyphIndex < row.glyphs.Count(); glyphIndex++)
                {
                    if (x + glyphHelper.Width >= rect.Width)
                        break;

                    Glyph glyph = row.glyphs[glyphIndex];

                    Brush background = glyph.highlighted ? HighlightBackground : glyph.background;
                    Brush foreground = glyph.highlighted ? HighlightForeground : glyph.background;

                    if (glyph.selected)
                    {
                        foreground = MixBrushes(SelectionForeground, foreground ?? Foreground);
                        background = MixBrushes(SelectionBackground, background ?? Background);
                    }

                    if (background != null)
                    {
                        GuidelineSet bgGuidelines = new GuidelineSet();
                        bgGuidelines.GuidelinesX.Add(x);
                        bgGuidelines.GuidelinesX.Add(x + glyphHelper.Width);
                        bgGuidelines.GuidelinesY.Add(y);
                        bgGuidelines.GuidelinesY.Add(y + glyphHelper.Height);

                        dc.PushGuidelineSet(bgGuidelines);
                        dc.DrawRectangle(background, null, new Rect(x, y, glyphHelper.Width, glyphHelper.Height));
                        dc.Pop();
                    }

                    Point point = new Point(x, y + glyphHelper.Baseline);
                    GlyphRun glyphRun = glyphHelper.CreateGlyphRun(point, new ushort[] { glyph.glyphIndex }, new double[] { glyphHelper.Width });

                    GuidelineSet guidelines = new GuidelineSet();
                    guidelines.GuidelinesX.Add(point.X);
                    guidelines.GuidelinesY.Add(point.Y);
                    dc.PushGuidelineSet(guidelines);
                    dc.DrawGlyphRun(foreground ?? Foreground, glyphRun);
                    dc.Pop();

                    x += glyphHelper.Width;
                }


                y += glyphHelper.Height;
            }
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            selectionDragStart = -1;
            UpdateSelection(0, 0);
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);

            if (IsKeyboardFocused == false)
                Keyboard.Focus(this);
        }

        int selectionDragStart = -1;
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            Point mouseDownPoint = e.GetPosition(this);
            int textPosition = GetPositionFromPoint(mouseDownPoint);
            if (e.ClickCount == 1)
            {
                selectionDragStart = textPosition;
                UpdateSelection(0, 0);
            }
            else if (e.ClickCount == 2)
            {
                selectionDragStart = -1;
                SelectWord(textPosition);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (e.LeftButton == MouseButtonState.Released || selectionDragStart == -1)
            {
                selectionDragStart = -1;
                return;
            }

            Point mousePoint = e.GetPosition(this);
            int newPosition = GetPositionFromPoint(mousePoint);

            if (selectionDragStart < newPosition)
                UpdateSelection(selectionDragStart, newPosition - selectionDragStart);
            else
                UpdateSelection(newPosition, selectionDragStart - newPosition);
        }

        protected void SelectWord(int position)
        {
            int rightPos = position;
            while (rightPos < Text.Length - 1 && Char.IsLetterOrDigit(Text[rightPos + 1]))
                rightPos++;

            int leftPos = position;
            while (leftPos > 0 && Char.IsLetterOrDigit(Text[leftPos - 1]))
                leftPos--;

            if (rightPos != leftPos)
                UpdateSelection(leftPos, rightPos - leftPos + 1);
        }

        protected int GetPositionFromPoint(Point mousePoint)
        {
            int row = (int)(mousePoint.Y / glyphHelper.Height) + _verticalOffset;

            int position = 0;
            for (int i = 0; i < row; i++)
                position += rows[i].TextLength;

            int column = (int)(mousePoint.X / glyphHelper.Width) + _horizontalOffset;
            position += Math.Min(column, rows[row].TextLength - 1);
            return position;
        }

        protected void GetRowFromPos(int textPos, out int row, out int posInRow)
        {
            row = 0;
            posInRow = 0;
            if (textPos <= 0)
                return;

            while (row < rows.Count())
            {
                posInRow += rows[row].TextLength;
                if (posInRow > textPos)
                {
                    posInRow = textPos - posInRow + rows[row].TextLength;
                    return;
                }

                row++;
            }
        }

        protected enum MarkType
        {
            Selected,
            Highlighted
        }

        protected void MarkSymbols(MarkType markType, bool value, int rowIndex, int startPos, int endPos = -1)
        {
            Row row = rows[rowIndex];
            if (endPos == -1)
                endPos = row.glyphs.Count();

            if (markType == MarkType.Selected)
            {
                while (startPos < endPos)
                    row.glyphs[startPos++].selected = value;
            }
            else
            {
                while (startPos < endPos)
                    row.glyphs[startPos++].highlighted = value;
            }
        }

        protected void MarkRange(MarkType markType, bool value, int start, int length)
        {
            if (length == 0)
                return;

            GetRowFromPos(start, out int startRow, out int startPosInRow);
            GetRowFromPos(start + length, out int endRow, out int endPosInRow);

            if (startRow != endRow)
            {
                MarkSymbols(markType, value, startRow, startPosInRow);
                for (int i = startRow + 1; i < endRow; i++)
                    MarkSymbols(markType, value, i, 0);
                MarkSymbols(markType, value, endRow, 0, endPosInRow);
            }
            else
            {
                MarkSymbols(markType, value, startRow, startPosInRow, endPosInRow);
            }
        }

        protected void UpdateSelection(int start, int length)
        {
            if (selectionStart == start && SelectionLength == length)
                return;

            MarkRange(MarkType.Selected, false, selectionStart, selectionLength);
            MarkRange(MarkType.Selected, true, start, length);

            selectionStart = start;
            selectionLength = length;
            selectedText = Text.Substring(start, length);

            InvalidateVisual();
        }

        protected void UpdateContent()
        {
            if (DisableFilterHighlighted || HighlightedText.Length <= 0)
            {
                rows = originalRows;
                text = originalText;
            }
            else
            {
                FilterHighlights();
            }

            MarkRange(MarkType.Highlighted, false, 0, Text.Length);
            if (EnableHighlight && HighlightedText.Length > 0)
                ApplyHighlights();

            if (WordWrap == true)
                WrapLines();

            _maxWidth = 0;
            foreach (var row in rows)
                _maxWidth = Math.Max(_maxWidth, row.TextLength - 1);
            

            UpdateScrollbarsRange();
            UpdateScrollbarsVisibility(ActualWidth, ActualHeight);
            InvalidateVisual();
        }

        protected void UpdateScrollbarsRange()
        {
            double hValue = _hScroll.Value;
            _hScroll.Maximum = Math.Max(0, _maxWidth - _hScroll.ViewportSize + 1);
            _hScroll.Value = hValue;

            double vValue = _vScroll.Value;
            _vScroll.Maximum = Math.Max(0, rows.Count() - _vScroll.ViewportSize + 1);
            _vScroll.Value = vValue;
        }

        protected void UpdateScrollbarsVisibility(double width, double height)
        {
            if (glyphHelper == null)
                return;

            if (_maxWidth * glyphHelper.Width >= width)
                _hScroll.Visibility = Visibility.Visible;
            else
                _hScroll.Visibility = Visibility.Collapsed;

            if (rows.Count() * glyphHelper.Height >= height)
                _vScroll.Visibility = Visibility.Visible;
            else
                _vScroll.Visibility = Visibility.Collapsed;
        }

        private void FilterHighlights()
        {
            string highlightedText = HighlightedText;
            var comparision = MatchCaseHighlighted ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            rows = new List<Row>();
            text = "";

            int textPos = 0;
            foreach (var row in originalRows)
            {
                int posInRow = textPos;
                int lastPosInRow = posInRow + row.TextLength;
                while (posInRow < lastPosInRow)
                {
                    if (String.Compare(originalText, posInRow, highlightedText, 0, highlightedText.Length, comparision) == 0)
                    {
                        if (row == originalRows.Last())
                            text += originalText.Substring(textPos, row.TextLength - 1);
                        else
                            text += originalText.Substring(textPos, row.TextLength);

                        rows.Add(row);
                        break;
                    }

                    posInRow++;
                }

                textPos += row.TextLength;
            }

            if (text.Length > 0 && text.Last() == '\n')
                rows.Add(new Row());
        }

        private void ApplyHighlights()
        {
            string highlightedText = HighlightedText;
            var comparision = MatchCaseHighlighted ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            int textPos = 0;
            while (textPos < Text.Length)
            {
                if (String.Compare(Text, textPos, highlightedText, 0, highlightedText.Length, comparision) == 0)
                {
                    MarkRange(MarkType.Highlighted, true, textPos, highlightedText.Length);
                    textPos += highlightedText.Length - 1;
                }

                textPos++;
            }
        }

        private void WrapLines()
        {
            rows = new List<Row>(rows);

            int textPos = 0;
            int rowIndex = 0;
            int maxWidth = (int)(ActualWidth / glyphHelper.Width);
            while(rowIndex < rows.Count())
            {
                Row row = rows[rowIndex];
                if (row.TextLength - 1 > maxWidth)
                {
                    int wrapPoint = maxWidth;
                    int wrapStopPoint = Math.Max(0, maxWidth - MaxWordWrapLenght);
                    while(wrapPoint > wrapStopPoint)
                    {
                        if (Char.IsWhiteSpace(row.glyphs[wrapPoint].symbol))
                            break;

                        wrapPoint--;
                    }

                    if (wrapPoint <= wrapStopPoint)
                        wrapPoint = maxWidth;

                    wrapPoint++;
                    text.Insert(textPos + wrapPoint, "\n");

                    rows[rowIndex] = new Row();
                    rows.Insert(rowIndex + 1, new Row());

                    int copyElementsCount = row.glyphs.Count - wrapPoint;
                    rows[rowIndex].glyphs.AddRange(row.glyphs.GetRange(0, wrapPoint - 1));
                    rows[rowIndex + 1].glyphs.AddRange(row.glyphs.GetRange(wrapPoint, copyElementsCount));
                }

                textPos += row.TextLength;
                rowIndex++;
            }
        }

        private static void OnHighlightTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var textBlock = (TextBlockEx)sender;
            textBlock.UpdateContent();
        }

        private static void OnWordWrapChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var textBlock = (TextBlockEx)sender;
            textBlock.UpdateContent();
        }

        private void OnHScrollValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _horizontalOffset = (int)(_hScroll.Value);
            InvalidateVisual();
        }

        private void OnVScrollValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _verticalOffset = (int)(_vScroll.Value);
            InvalidateVisual();
        }

        private Brush MixBrushes(Brush brush1, Brush brush2)
        {
            if (brush1 is SolidColorBrush && brush2 is SolidColorBrush)
            {
                var solidBrush1 = (SolidColorBrush)brush1;
                var solidBrush2 = (SolidColorBrush)brush2;
                return new SolidColorBrush(Color.FromArgb(Math.Max(solidBrush1.Color.A, solidBrush2.Color.A),
                                                          (byte)((solidBrush1.Color.R + solidBrush2.Color.R) / 2),
                                                          (byte)((solidBrush1.Color.G + solidBrush2.Color.G) / 2),
                                                          (byte)((solidBrush1.Color.B + solidBrush2.Color.B) / 2)));
            }

            if (brush1 == null)
                return brush2;
            else
                return brush1;
        }
    }
}
