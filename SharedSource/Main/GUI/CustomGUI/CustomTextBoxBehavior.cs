using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using WaveEngine.Common.Input;
using WaveEngine.Common.Math;
using WaveEngine.Components.Gestures;
using WaveEngine.Components.UI;
using WaveEngine.Framework;
using WaveEngine.Framework.Animation;
using WaveEngine.Framework.Graphics;
using WaveEngine.Framework.Services;

namespace Codigo.GUI
{
    public class CustomTextBoxBehavior:FocusBehavior
    {    /// <summary>
         /// The input service
         /// </summary>
        private Input inputService;
        /// <summary>
        /// The text control
        /// </summary>
        private TextControl textControl;
        /// <summary>
        /// The panel
        /// </summary>
        [RequiredComponent]
        public Transform2D Transform;
        /// <summary>
        /// The gestures
        /// </summary>
        [RequiredComponent]
        public TouchGestures Gestures;
        /// <summary>
        /// The cursor control
        /// </summary>
        private Transform2D cursorTransform;

        /// <summary>
        /// Two part of text
        /// </summary>
        private string textBeforeCursor= string.Empty, textAfterCursor= string.Empty;
        /// <summary>
        /// The cursor animation
        /// </summary>
        private AnimationUI cursorAnimation;

        /// <summary>
        /// The flicker animation
        /// </summary>
        private SingleAnimation flicker;

        /// <summary>
        /// The before keyboard state
        /// </summary>
        private KeyboardState beforeKeyboardState;
        /**
         * <summary>
         * Whether this is numeric or not
         * </summary>
         */
        private bool isNumeric = false;
        /**
         * <summary>
         * Max character available
         * </summary>
         */
        private int maxLength = 10;

        /// <summary>
        /// The uppercase actived
        /// </summary>
        private bool uppercase;

        /// <summary>
        /// The alt-case actived
        /// </summary>
        private bool altcase;

        /// <summary>
        /// Gets or sets a value indicating the number of characters allowed. Default 10.
        /// </summary>
     
        public int MaxLength
        {
            get { return this.maxLength; }
            set { this.maxLength = value; }
        }
        /// <summary>
        /// Gets or sets a value indicating whether this instance allows only numeric characters.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is numeric; otherwise, <c>false</c>.
        /// </value>
        public bool IsNumeric
        {
            get { return this.isNumeric; }
            set { this.isNumeric = value; }
        }
        public CustomTextBoxBehavior() : base("NumericTextBoxBehavior")
        {
        }


        /// <summary>
        /// Sets the update text.
        /// </summary>
        /// <value>
        /// The update text.
        /// </value>
        public string UpdateText
        {
            set
            {
                this.textBeforeCursor = value;
                this.textAfterCursor = string.Empty;
            }
        }

        protected override void Initialize()
        {
            base.Initialize();

            this.inputService = WaveServices.Input;
            this.flicker = new SingleAnimation(1, 0, new Duration(TimeSpan.FromSeconds(0.4f)));

            this.Gestures.TouchPressed -= this.Gestures_TouchPressed;
            this.Gestures.TouchPressed += this.Gestures_TouchPressed;
            
        }

        /// <summary>
        /// Handles the TouchPressed event of the Gestures control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GestureEventArgs" /> instance containing the event data.</param>
        ///
        private void Gestures_TouchPressed(object sender, GestureEventArgs e)
        {
           
            this.IsFocus = true;

            if (this.inputService.KeyboardState.IsConnected)
            {
                float clickX = e.GestureSample.Position.X - this.Transform.Rectangle.X;
                float clickY = e.GestureSample.Position.Y - this.Transform.Rectangle.Y;

                NewPosition(clickX, clickY);
            }

        }

        private void NewPosition(float clickX, float clickY)
        {
            // Cursor position Y
            float posY = this.textControl.LineSpacing;
            int lineIndex = 0;
            while (lineIndex < this.textControl.LinesInfo.Count - 1 &&
                   posY + this.textControl.FontHeight < clickY)
            {
                posY += this.textControl.FontHeight + this.textControl.LineSpacing;
                lineIndex++;
            }

            // Cursor position X
            float posX = 0;
            if (this.textControl.LinesInfo.Count > 0)
            {
                LineInfo lineInfo = this.textControl.LinesInfo[lineIndex];
                if (posY + this.textControl.FontHeight < clickY)
                {
                    // Outside of textControl
                    posX = lineInfo.AlignmentOffsetX + lineInfo.Size.X;
                    this.textBeforeCursor = this.textControl.Text;
                    this.textAfterCursor = string.Empty;
                }
                else
                {
                    // Search X position
                    posX = lineInfo.AlignmentOffsetX;
                    float maxOffsetX = lineInfo.AlignmentOffsetX + lineInfo.Size.X;
                    int characterIndex = 0;
                    string currentLineText = lineInfo.SubTextList[0].Text;
                    float characterOffset = this.textControl.SpriteFont.MeasureString(currentLineText[characterIndex].ToString()).X;
                    while (posX + (characterOffset / 2) < clickX)
                    {
                        posX += characterOffset;
                        if (characterIndex < currentLineText.Length - 1)
                        {
                            characterIndex++;
                            characterOffset = this.textControl.SpriteFont.MeasureString(currentLineText[characterIndex].ToString()).X;
                        }
                        else
                        {
                            //Its the last one
                            characterIndex++;
                            break;
                        }
                    }

                    // Text before cursor
                    List<LineInfo> linesInfo = this.textControl.LinesInfo;
                    this.textBeforeCursor = string.Empty;
                    for (int i = 0; i < lineIndex; i++)
                    {
                        this.textBeforeCursor += this.textControl.LinesInfo[i].SubTextList[0].Text;
                    }

                    this.textBeforeCursor += currentLineText.Substring(0, characterIndex);

                    // Text After cursor
                    this.textAfterCursor = currentLineText.Substring(characterIndex);
                    for (int i = lineIndex + 1; i < this.textControl.LinesInfo.Count; i++)
                    {
                        this.textAfterCursor += this.textControl.LinesInfo[i].SubTextList[0].Text;
                    }
                }
            }

            // Final positions
            this.cursorTransform.X = posX;
            this.cursorTransform.Y = posY;
            this.cursorAnimation.BeginAnimation(Transform2D.OpacityProperty, this.flicker);
        }


        /// <summary>
        /// Resolves the dependencies needed for this instance to work.
        /// </summary>
        protected override void ResolveDependencies()
        {
            base.ResolveDependencies();

            this.textControl = Owner.FindChild("TextEntity").FindComponent<TextControl>();
            this.textBeforeCursor = this.textControl.Text;

            Entity cursorEntity = Owner.FindChild("CursorEntity");
            this.cursorTransform = cursorEntity.FindComponent<Transform2D>();
            this.cursorAnimation = cursorEntity.FindComponent<AnimationUI>();


        }

        protected override void Update(TimeSpan gameTime)
        {
            if (this.inputService.KeyboardState.IsConnected && this.IsFocus)
            {
                if (cursorAnimation.State == AnimationState.Stopped)
                {
                    this.cursorAnimation.BeginAnimation(Transform2D.OpacityProperty, this.flicker);
                }

                if (textControl.Text != textBeforeCursor + textAfterCursor)
                {
                    textControl.Text = textBeforeCursor + textAfterCursor;
                }
                if (textControl.Text.Length < maxLength)
                {
                    if (this.inputService.KeyboardState.Number1 == ButtonState.Pressed &&
                    this.beforeKeyboardState.Number1 != ButtonState.Pressed)
                    {
                        this.AppendCharacter('1');
                    }
                    else if (this.inputService.KeyboardState.Number2 == ButtonState.Pressed &&
                        this.beforeKeyboardState.Number2 != ButtonState.Pressed)
                    {
                        this.AppendCharacter('2');
                    }
                    else if (this.inputService.KeyboardState.Number3 == ButtonState.Pressed &&
                        this.beforeKeyboardState.Number3 != ButtonState.Pressed)
                    {
                        this.AppendCharacter('3');
                    }
                    else if (this.inputService.KeyboardState.Number4 == ButtonState.Pressed &&
                        this.beforeKeyboardState.Number4 != ButtonState.Pressed)
                    {
                        this.AppendCharacter('4');
                    }
                    else if (this.inputService.KeyboardState.Number5 == ButtonState.Pressed &&
                        this.beforeKeyboardState.Number5 != ButtonState.Pressed)
                    {
                        this.AppendCharacter('5');
                    }
                    else if (this.inputService.KeyboardState.Number6 == ButtonState.Pressed &&
                        this.beforeKeyboardState.Number6 != ButtonState.Pressed)
                    {
                        this.AppendCharacter('6');
                    }
                    else if (this.inputService.KeyboardState.Number7 == ButtonState.Pressed &&
                        this.beforeKeyboardState.Number7 != ButtonState.Pressed)
                    {
                        this.AppendCharacter('7');
                    }
                    else if (this.inputService.KeyboardState.Number8 == ButtonState.Pressed &&
                        this.beforeKeyboardState.Number8 != ButtonState.Pressed)
                    {
                        this.AppendCharacter('8');
                    }
                    else if (this.inputService.KeyboardState.Number9 == ButtonState.Pressed &&
                        this.beforeKeyboardState.Number9 != ButtonState.Pressed)
                    {
                        this.AppendCharacter('9');
                    }
                    else if (this.inputService.KeyboardState.Number0 == ButtonState.Pressed &&
                        this.beforeKeyboardState.Number0 != ButtonState.Pressed)
                    {
                        this.AppendCharacter('0');
                    }

                    if (!isNumeric)
                    {
                        if (this.inputService.KeyboardState.A == ButtonState.Pressed &&
                        this.beforeKeyboardState.A != ButtonState.Pressed)
                        {
                            this.AppendCharacter('a');
                        }
                        else if (this.inputService.KeyboardState.B == ButtonState.Pressed &&
                            this.beforeKeyboardState.B != ButtonState.Pressed)
                        {
                            this.AppendCharacter('b');
                        }
                        else if (this.inputService.KeyboardState.C == ButtonState.Pressed &&
                            this.beforeKeyboardState.C != ButtonState.Pressed)
                        {
                            this.AppendCharacter('c');
                        }
                        else if (this.inputService.KeyboardState.D == ButtonState.Pressed &&
                            this.beforeKeyboardState.D != ButtonState.Pressed)
                        {
                            this.AppendCharacter('d');
                        }
                        else if (this.inputService.KeyboardState.E == ButtonState.Pressed &&
                            this.beforeKeyboardState.E != ButtonState.Pressed)
                        {
                            this.AppendCharacter('e');
                        }
                        else if (this.inputService.KeyboardState.F == ButtonState.Pressed &&
                            this.beforeKeyboardState.F != ButtonState.Pressed)
                        {
                            this.AppendCharacter('f');
                        }
                        else if (this.inputService.KeyboardState.G == ButtonState.Pressed &&
                            this.beforeKeyboardState.G != ButtonState.Pressed)
                        {
                            this.AppendCharacter('g');
                        }
                        else if (this.inputService.KeyboardState.H == ButtonState.Pressed &&
                            this.beforeKeyboardState.H != ButtonState.Pressed)
                        {
                            this.AppendCharacter('h');
                        }
                        else if (this.inputService.KeyboardState.I == ButtonState.Pressed &&
                            this.beforeKeyboardState.I != ButtonState.Pressed)
                        {
                            this.AppendCharacter('i');
                        }
                        else if (this.inputService.KeyboardState.Grave == ButtonState.Pressed &&
                            this.beforeKeyboardState.Grave != ButtonState.Pressed)
                        {
                            this.AppendCharacter('º');
                        }
                        else if (this.inputService.KeyboardState.J == ButtonState.Pressed &&
                           this.beforeKeyboardState.J != ButtonState.Pressed)
                        {
                            this.AppendCharacter('j');
                        }
                        else if (this.inputService.KeyboardState.K == ButtonState.Pressed &&
                           this.beforeKeyboardState.K != ButtonState.Pressed)
                        {
                            this.AppendCharacter('k');
                        }
                        else if (this.inputService.KeyboardState.L == ButtonState.Pressed &&
                           this.beforeKeyboardState.L != ButtonState.Pressed)
                        {
                            this.AppendCharacter('l');
                        }
                        else if (this.inputService.KeyboardState.M == ButtonState.Pressed &&
                           this.beforeKeyboardState.M != ButtonState.Pressed)
                        {
                            this.AppendCharacter('m');
                        }
                        else if (this.inputService.KeyboardState.N == ButtonState.Pressed &&
                           this.beforeKeyboardState.N != ButtonState.Pressed)
                        {
                            this.AppendCharacter('n');
                        }
                        else if (this.inputService.KeyboardState.O == ButtonState.Pressed &&
                           this.beforeKeyboardState.O != ButtonState.Pressed)
                        {
                            this.AppendCharacter('o');
                        }
                        else if (this.inputService.KeyboardState.P == ButtonState.Pressed &&
                           this.beforeKeyboardState.P != ButtonState.Pressed)
                        {
                            this.AppendCharacter('p');
                        }
                        else if (this.inputService.KeyboardState.Q == ButtonState.Pressed &&
                           this.beforeKeyboardState.Q != ButtonState.Pressed)
                        {
                            this.AppendCharacter('q');
                        }
                        else if (this.inputService.KeyboardState.R == ButtonState.Pressed &&
                           this.beforeKeyboardState.R != ButtonState.Pressed)
                        {
                            this.AppendCharacter('r');
                        }
                        else if (this.inputService.KeyboardState.S == ButtonState.Pressed &&
                           this.beforeKeyboardState.S != ButtonState.Pressed)
                        {
                            this.AppendCharacter('s');
                        }
                        else if (this.inputService.KeyboardState.T == ButtonState.Pressed &&
                           this.beforeKeyboardState.T != ButtonState.Pressed)
                        {
                            this.AppendCharacter('t');
                        }
                        else if (this.inputService.KeyboardState.U == ButtonState.Pressed &&
                           this.beforeKeyboardState.U != ButtonState.Pressed)
                        {
                            this.AppendCharacter('u');
                        }
                        else if (this.inputService.KeyboardState.V == ButtonState.Pressed &&
                           this.beforeKeyboardState.V != ButtonState.Pressed)
                        {
                            this.AppendCharacter('v');
                        }
                        else if (this.inputService.KeyboardState.W == ButtonState.Pressed &&
                           this.beforeKeyboardState.W != ButtonState.Pressed)
                        {
                            this.AppendCharacter('w');
                        }
                        else if (this.inputService.KeyboardState.X == ButtonState.Pressed &&
                           this.beforeKeyboardState.X != ButtonState.Pressed)
                        {
                            this.AppendCharacter('x');
                        }
                        else if (this.inputService.KeyboardState.Y == ButtonState.Pressed &&
                           this.beforeKeyboardState.Y != ButtonState.Pressed)
                        {
                            this.AppendCharacter('y');
                        }
                        else if (this.inputService.KeyboardState.Z == ButtonState.Pressed &&
                           this.beforeKeyboardState.Z != ButtonState.Pressed)
                        {
                            this.AppendCharacter('z');
                        }
                        else if (this.inputService.KeyboardState.Space == ButtonState.Pressed &&
                           this.beforeKeyboardState.Space != ButtonState.Pressed)
                        {
                            this.AppendCharacter(' ');
                        }
                    }
                }
                if (this.inputService.KeyboardState.Subtract == ButtonState.Pressed &&
                    this.beforeKeyboardState.Subtract != ButtonState.Pressed)
                {
                    this.RemoveBackCharacter();
                }
                if (this.inputService.KeyboardState.Back == ButtonState.Pressed &&
                    this.beforeKeyboardState.Back != ButtonState.Pressed)
                {
                    this.RemoveBackCharacter();
                }
                if(this.inputService.KeyboardState.Left == ButtonState.Pressed &&
                    this.beforeKeyboardState.Left != ButtonState.Pressed)
                {
                    this.MoveLeft();
                }
                if (this.inputService.KeyboardState.Right == ButtonState.Pressed &&
                    this.beforeKeyboardState.Right != ButtonState.Pressed)
                {
                    this.MoveRight();
                }

                // Supr Key
                if (this.inputService.KeyboardState.Delete == ButtonState.Pressed && this.beforeKeyboardState.Delete != ButtonState.Pressed)
                {
                    this.RemoveFrontCharacter();
                }

                // Special keys
                if ((this.inputService.KeyboardState.LeftShift == ButtonState.Pressed && this.beforeKeyboardState.LeftShift != ButtonState.Pressed) ||
                     (this.inputService.KeyboardState.RightShift == ButtonState.Pressed && this.beforeKeyboardState.RightShift != ButtonState.Pressed) ||
                     (this.inputService.KeyboardState.CapitalLock == ButtonState.Pressed && this.beforeKeyboardState.CapitalLock != ButtonState.Pressed && !this.uppercase))
                {
                    this.uppercase = true;
                }
                else if ((this.inputService.KeyboardState.LeftShift == ButtonState.Release && this.beforeKeyboardState.LeftShift != ButtonState.Release) ||
                     (this.inputService.KeyboardState.RightShift == ButtonState.Release && this.beforeKeyboardState.RightShift != ButtonState.Release) ||
                     (this.inputService.KeyboardState.CapitalLock == ButtonState.Pressed && this.beforeKeyboardState.CapitalLock != ButtonState.Pressed && this.uppercase))
                {
                    this.uppercase = false;
                }

                // Combinate Alt
                if (this.inputService.KeyboardState.RightAlt == ButtonState.Pressed && this.beforeKeyboardState.RightAlt != ButtonState.Pressed)
                {
                    this.altcase = true;
                }
                else if (this.inputService.KeyboardState.RightAlt == ButtonState.Release)
                {
                    this.altcase = false;
                }
            }
        

            this.beforeKeyboardState = this.inputService.KeyboardState;
        }

        private void MoveRight()
        {
            if (textAfterCursor.Length > 0)
            {
                textBeforeCursor = textBeforeCursor + textAfterCursor[0];
                textAfterCursor = textAfterCursor.Substring(1);
                textControl.Text = textBeforeCursor + textAfterCursor;
            }
            UpdateCursor();
        }

        private void MoveLeft()
        {
            if (textBeforeCursor.Length > 0)
            {
                textAfterCursor = textBeforeCursor[textBeforeCursor.Length - 1] + textAfterCursor;
                textBeforeCursor = textBeforeCursor.Substring(0, textBeforeCursor.Length - 1);
                textControl.Text = textBeforeCursor + textAfterCursor;
            }
            UpdateCursor();
        }
        private void UpdateCursor()
        {
            Vector2 dist = this.textControl.SpriteFont.MeasureString(textBeforeCursor);

            NewPosition(dist.X, dist.Y);
        }


        /// <summary>
        /// Appends the text.
        /// </summary>
        /// <param name="c">The c.</param>
        private void AppendCharacter(char c)
        {

            char character = c;

            if (!isNumeric)
            {

                if (this.uppercase)
                {
                    if (c == 'º')
                    {
                        character = 'ª';
                    }
                    else if (c == '1')
                    {
                        character = '!';
                    }
                    else if (c == '2')
                    {
                        character = '"';
                    }
                    else if (c == '3')
                    {
                        character = '·';
                    }
                    else if (c == '4')
                    {
                        character = '$';
                    }
                    else if (c == '5')
                    {
                        character = '%';
                    }
                    else if (c == '6')
                    {
                        character = '&';
                    }
                    else if (c == '7')
                    {
                        character = '/';
                    }
                    else if (c == '8')
                    {
                        character = '(';
                    }
                    else if (c == '9')
                    {
                        character = ')';
                    }
                    else if (c == '0')
                    {
                        character = '=';
                    }
                    else
                    {
                        character = char.ToUpper(c);
                    }
                }

                if (this.altcase)
                {
                    if (c == 'º')
                    {
                        character = '\\';
                    }
                    else if (c == '1')
                    {
                        character = '|';
                    }
                    else if (c == '2')
                    {
                        character = '@';
                    }
                    else if (c == '3')
                    {
                        character = '#';
                    }
                    else if (c == '4')
                    {
                        character = '~';
                    }
                    else if (c == '5')
                    {
                        character = '€';
                    }
                    else if (c == '6')
                    {
                        character = '¬';
                    }
                }
            }
            this.textControl.Text = this.textBeforeCursor + character + this.textAfterCursor;
            this.textBeforeCursor += character;

            UpdateCursor();
        }
        /// <summary>
        /// Removes the character.
        /// </summary>
        private void RemoveBackCharacter()
        {
            
            string text = this.textBeforeCursor;

            if (text.Length < 2)
            {
                this.textBeforeCursor = string.Empty;
            }
            else if (text.Length > 4)
            {
                string fourLast = text.Substring(text.Length - 4);
                if (fourLast.Equals(" /n "))
                {
                    this.textBeforeCursor = text.Substring(0, text.Length - 4);
                }
                else
                {
                    this.textBeforeCursor = text.Substring(0, text.Length - 1);
                }
            }
            else
            {
                this.textBeforeCursor = text.Substring(0, text.Length - 1);
            }

            this.textControl.Text = this.textBeforeCursor + this.textAfterCursor;
            UpdateCursor();
        }
        /// <summary>
        /// Removes the front character.
        /// </summary>
        private void RemoveFrontCharacter()
        {
            if (this.textAfterCursor.Length > 0)
            {
                this.textAfterCursor = this.textAfterCursor.Substring(1);
                this.textControl.Text = this.textBeforeCursor + this.textAfterCursor;  
            }
        }
    }
}
