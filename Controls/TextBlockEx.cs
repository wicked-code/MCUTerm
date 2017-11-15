using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MCUTerm.Controls
{
    public partial class TextBlockEx : TextBlock
    {
        protected int selectionStart;
        protected int selectionLength;
        protected string selectedText;

        protected List<Run> runList = new List<Run>();

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
                                      new FrameworkPropertyMetadata((Brush)null, OnSelectionBrushChanged));

        public Brush SelectionForeground
        {
            get => (Brush)base.GetValue(SelectionForegroundProperty);
            set => base.SetValue(SelectionForegroundProperty, value);
        }

        public static readonly DependencyProperty SelectionForegroundProperty =
          DependencyProperty.Register("SelectionForeground", typeof(Brush), typeof(TextBlockEx),
                                      new FrameworkPropertyMetadata((Brush)null, OnSelectionBrushChanged));

        public Brush HighlightBackground
        {
            get => (Brush)base.GetValue(HighlightBackgroundProperty);
            set => base.SetValue(HighlightBackgroundProperty, value);
        }

        public static readonly DependencyProperty HighlightBackgroundProperty =
          DependencyProperty.Register("HighlightBackground", typeof(Brush), typeof(TextBlockEx),
                                      new FrameworkPropertyMetadata((Brush)null, OnHighlightTextChanged));

        public Brush HighlightForeground
        {
            get => (Brush)base.GetValue(HighlightForegroundProperty);
            set => base.SetValue(HighlightForegroundProperty, value);
        }

        public static readonly DependencyProperty HighlightForegroundProperty =
          DependencyProperty.Register("HighlightForeground", typeof(Brush), typeof(TextBlockEx),
                                      new FrameworkPropertyMetadata((Brush)null, OnHighlightTextChanged));

        public int TabIndex
        {
            get { return (int)GetValue(TabIndexProperty); }
            set { SetValue(TabIndexProperty, value); }
        }

        public static readonly DependencyProperty TabIndexProperty =
            KeyboardNavigation.TabIndexProperty.AddOwner(typeof(TextBlockEx));

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

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy,
                (sender, e) => { Clipboard.SetText(((TextBlockEx)sender).SelectedText); },
                (sender, e) => { e.CanExecute = ((TextBlockEx)sender).SelectedText.Length > 0; }
            ));

            CommandBindings.Add(new CommandBinding(ApplicationCommands.SelectAll,
                (sender, e) => { ((TextBlockEx)sender).Select(0, ((TextBlockEx)sender).Text.Length); },
                (sender, e) => { e.CanExecute = ((TextBlockEx)sender).Text.Length > 0; }
            ));
        }

        public void ClearText()
        {
            runList.Clear();
            UpdateSelection(0, 0);
            UpdateContent();
        }

        public void AddText(string text)
        {
            AddText(text, null, null);
        }

        public void AddText(string text, Brush background, Brush foreground)
        {
            if (text.Length == 0)
                return;

            Run run = runList.LastOrDefault();
            if (run == null || run.Background != background || run.Foreground != foreground)
            {
                run = CreateRun(text, background, foreground);
                runList.Add(run);
            }
            else
            {
                run.Text += text;
            }

            UpdateContent();
        }

        public void Select(int start, int length)
        {
            if (length == 0 && SelectionLength == 0)
                return;

            UpdateSelection(start, length);
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
            int textPosition = GetTextPosition(GetPositionFromPoint(mouseDownPoint, true));
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
            int newPosition = GetTextPosition(GetPositionFromPoint(mousePoint, true));

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

        protected int GetTextPosition(TextPointer position)
        {
            var result = 0;
            while(position != null)
            {
                result += position.GetTextRunLength(LogicalDirection.Backward);
                position = position.GetNextContextPosition(LogicalDirection.Backward);
            }

            return result;
        }

        protected void UpdateSelection(int start, int length)
        {
            if (selectionStart == start && SelectionLength == length)
                return;

            selectionStart = start;
            selectionLength = length;
            selectedText = Text.Substring(start, length);

            UpdateContent();
        }

        private static void OnSelectionBrushChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var textBlock = (TextBlockEx)sender;
            if (textBlock.SelectionLength != 0)
                textBlock.UpdateContent();
        }

        private static void OnHighlightTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var textBlock = (TextBlockEx)sender;
            textBlock.UpdateContent();
        }

        protected void UpdateContent()
        {
            var newRuns = new List<Run>();

            if (HighlightedText.Length > 0)
                ApplyHighlights(newRuns);
            else
                CloneRunList(newRuns);

            if (selectionLength > 0)
                ApplySelection(newRuns);

            InlineCollection inlines = Inlines;
            inlines.Clear();
            inlines.AddRange(newRuns);
        }

        private void CloneRunList(List<Run> runs)
        {
            foreach (var run in runList)
                runs.Add(CreateRun(run.Text, run.Background, run.Foreground));
        }

        protected void ApplyHighlights(List<Run> runs)
        {
            var comparision = MatchCaseHighlighted ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            Brush background = EnableHighlight ? HighlightBackground : null;
            Brush foreground = EnableHighlight ? HighlightForeground : null;

            string highlightedText = HighlightedText;
            bool skipLineBreak = DisableFilterHighlighted;

            var skippedRuns = new List<Run>();
            foreach (var run in runList)
            {
                string runText = run.Text;
                if (runText.Length == 0)
                    continue;

                int textPos = 0;
                int startTextPos = 0;
                do
                {
                    if (runText[textPos] == '\n' || runText[textPos] == '\r')
                    {
                        if (textPos + 1 < runText.Length &&
                            ((runText[textPos] == '\n' && runText[textPos + 1] == '\r') ||
                             (runText[textPos] == '\r' && runText[textPos + 1] == '\n')))
                        {
                            textPos++;
                        }

                        if (skipLineBreak)
                        {
                            runs.AddRange(skippedRuns);
                            skippedRuns.Clear();

                            if (textPos - startTextPos + 1 > 0)
                                runs.Add(CreateRun(runText.Substring(startTextPos, textPos - startTextPos + 1), run.Background, run.Foreground));
                        }
                        else
                            skippedRuns.Clear();

                        startTextPos = textPos + 1;
                        skipLineBreak = DisableFilterHighlighted;
                    }

                    if (String.Compare(runText, textPos, highlightedText, 0, highlightedText.Length, comparision) == 0)
                    {
                        runs.AddRange(skippedRuns);
                        skippedRuns.Clear();

                        if (textPos - startTextPos > 0)
                        {
                            runs.Add(CreateRun(runText.Substring(startTextPos, textPos - startTextPos),
                                               run.Background, run.Foreground));
                        }

                        runs.Add(CreateRun(runText.Substring(textPos, HighlightedText.Length),
                                           background ?? run.Background, foreground ?? run.Foreground));

                        textPos += highlightedText.Length;
                        startTextPos = textPos;

                        skipLineBreak = true;
                    }
                    else
                        textPos++;
                }
                while (textPos < runText.Length);

                if (startTextPos < runText.Length)
                    skippedRuns.Add(CreateRun(runText.Substring(startTextPos), run.Background, run.Foreground));
            }

            runs.AddRange(skippedRuns);
        }

        protected void ApplySelection(List<Run> runs)
        {
            int textPos = 0;
            for (int runIndex = 0; runIndex < runs.Count(); runIndex++)
            {
                Run run = runs[runIndex];
                string runText = run.Text;

                int selectionEnd = selectionStart + selectionLength;
                if (selectionStart > textPos && selectionStart < textPos + runText.Length)
                {
                    int pos = selectionStart - textPos;
                    run.Text = runText.Substring(0, pos);

                    runIndex++;
                    if (selectionEnd < textPos + runText.Length)
                    {
                        runs.Insert(runIndex, CreateRun(runText.Substring(pos, selectionLength), 
                                                        MixBrushes(SelectionBackground, run.Background),
                                                        MixBrushes(SelectionForeground, run.Foreground)));

                        runs.Insert(runIndex + 1, CreateRun(runText.Substring(pos + selectionLength),
                                                            run.Background, run.Foreground));
                        break;
                    }
                    else
                        runs.Insert(runIndex, CreateRun(runText.Substring(pos),
                                                        MixBrushes(SelectionBackground, run.Background),
                                                        MixBrushes(SelectionForeground, run.Foreground)));
                }
                else if (selectionEnd > textPos && selectionEnd < textPos + runText.Length)
                {
                    int pos = selectionEnd - textPos;
                    run.Text = runText.Substring(pos);

                    runs.Insert(runIndex, CreateRun(runText.Substring(0, pos),
                                                    MixBrushes(SelectionBackground, run.Background),
                                                    MixBrushes(SelectionForeground, run.Foreground)));
                    break;
                }
                else if (selectionStart <= textPos && selectionEnd >= textPos + runText.Length)
                {
                    run.Background = MixBrushes(SelectionBackground, run.Background);
                    run.Foreground = MixBrushes(SelectionForeground, run.Foreground);
                }

                textPos += runText.Length;
            }
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

        private Run CreateRun(string Text, Brush background, Brush forground)
        {
            return new Run(Text)
            {
                Background = background ?? Background,
                Foreground = forground ?? Foreground
            };
        }
    }

}
