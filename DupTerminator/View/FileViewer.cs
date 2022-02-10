using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing;
using System.Diagnostics;

namespace DupTerminator.View
{
    class FileViewer : UserControl//Panel
    {
        private const int MAX_PATH = 260;

        private string _imagePath;
        //private Size _neighbourSizeMax;
        private int _currentImageWidth;
        private int _currentImageHeight;

        private Rectangle m_bitmapRect;
        private MemoryStream _memoryStream;
        private Bitmap m_bitmap;
        private bool _animationEnable = false;
        private bool _currentlyAnimating = false;

        private int _charHeight = 15;
        private ToolTip toolTip1;
        private System.ComponentModel.IContainer components;
        private int _charWidth = 9;

        public FileViewer()
        {
            InitializeComponents();
            //m_options.resultsOptions.OnImageViewChange += new ResultsOptions.ImageViewChangeHandler(OnImageViewChange);

            /*SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);*/
        }

        private void InitializeComponents()
        {
            Location = new System.Drawing.Point(0, 0);
            //Dock = DockStyle.Fill;
            BorderStyle = BorderStyle.Fixed3D;
            //BackColor = Color.DarkGray;
            DoubleBuffered = true;

            DoubleClick += new EventHandler(OnImageDoubleClicked);
            SizeChanged += new EventHandler(OnSizeChanged);

            Font = new Font(FontFamily.GenericMonospace, 10);
            //AutoScroll = true;

            Anchor = System.Windows.Forms.AnchorStyles.Left;
            //Location = new System.Drawing.Point(1, 168);
            //Size = new System.Drawing.Size(146, 145);
            //TabIndex = 6;
            //TabStop = false;
            toolTip1 = new ToolTip();
            toolTip1.AutomaticDelay = 100;
        }

        public void UpdateImage(string imagePath)
        {
            _imagePath = imagePath;
            if (imagePath != null)
            {
                toolTip1.SetToolTip(this, imagePath);
                StopAnimate();
                if (imagePath.Length < MAX_PATH)
                {
                    try
                    {
                        if (LoadFileToMemoryStream(imagePath))
                        {
                            m_bitmap = new Bitmap(_memoryStream);
                            AutoScroll = false;
                            _currentImageWidth = m_bitmap.Width;
                            _currentImageHeight = m_bitmap.Height;
                            _animationEnable = ImageAnimator.CanAnimate(m_bitmap);
                            UpdateImagePadding();
                            if (_animationEnable)
                                _currentlyAnimating = false;
                        }
                        else
                        {
                            m_bitmap = null;
                        }
                    }
                    catch //не удалось создать Bitmap, показываем в виде текста
                    {
                        m_bitmap = null;
                        //m_bitmap = LoadBitmap(imagePath);
                        AutoScroll = true;
                        CalculateAutoScrollSize();
                    }
                }
                else
                {
                    m_bitmap = new Bitmap(_memoryStream);
                }
                Refresh();
            }
        }

        private bool LoadFileToMemoryStream(string path)
        {
            if (_memoryStream != null)
            {
                _memoryStream.Close();
                _memoryStream.Dispose();
                _memoryStream = null;
            }
            FileInfo fileInfo = new FileInfo(path);
            if (fileInfo.Exists && (fileInfo.Length < Settings.GetInstance().Fields.MaxFilePreviewMb * 1024 * 1024))
            {
                try
                {
                    FileStream fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read);
                    byte[] buffer = new byte[fileStream.Length];
                    fileStream.Read(buffer, 0, buffer.Length);
                    fileStream.Close();
                    _memoryStream = new MemoryStream(buffer);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }


        private void AnimateImage()
        {
            if (!_currentlyAnimating)
            {
                ImageAnimator.Animate(m_bitmap, new EventHandler(OnFrameChanged));
                _currentlyAnimating = true;
            }
        }

        private void StopAnimate()
        {
            _animationEnable = false;
            ImageAnimator.StopAnimate(m_bitmap, new EventHandler(OnFrameChanged));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            //base.OnPaint(e);
            if (_animationEnable)
            {
                try
                {
                    AnimateImage();
                    ImageAnimator.UpdateFrames();
                }
                catch
                {
                    _animationEnable = false;
                }
            }
            if (m_bitmap != null)
            {
                e.Graphics.DrawImage(m_bitmap, m_bitmapRect);
            }
            else if (_memoryStream != null)
            {
                int symbolsPerLine = ClientRectangle.Width / _charWidth;
                int linesPerPage = ClientRectangle.Height / _charHeight;
                int startPos = VerticalScroll.Value * symbolsPerLine;
                _memoryStream.Seek(startPos, SeekOrigin.Begin);
                byte[] buffer = new byte[symbolsPerLine];
                for (int y = 0; y < linesPerPage; y++)
                    if (_memoryStream.Position < _memoryStream.Length)
                    {
                        int count = _memoryStream.Read(buffer, 0, buffer.Length);
                        char[] chars = Encoding.Default.GetChars(buffer, 0, count);
                        for (int x = 0; x < chars.Length; x++)
                            e.Graphics.DrawString(chars[x].ToString(), Font, Brushes.Black, x * _charWidth, y * _charHeight);
                    }
            }
        }

        private void OnFrameChanged(object sender, EventArgs e)
        {
            Invalidate();
        }

        private void OnImageDoubleClicked(object sender, System.EventArgs e)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = _imagePath;
            try
            {
                Process.Start(startInfo);
            }
            catch (System.Exception exeption)
            {
                MessageBox.Show(exeption.Message);
            }
        }

        private void OnSizeChanged(object sender, EventArgs e)
        {
            //UpdateImagePadding(_neighbourSizeMax);
            UpdateImagePadding();
            CalculateAutoScrollSize();
        }

        private void CalculateAutoScrollSize()
        {
            if (_memoryStream != null && ClientRectangle.Width * ClientRectangle.Height > 0 && m_bitmap == null)
            {
                int symbolsPerPageLine = ClientRectangle.Width / _charWidth;
                int linesPerPage = ClientRectangle.Height / _charHeight;
                int linesCount = (symbolsPerPageLine > 0) ? (int)_memoryStream.Length / symbolsPerPageLine : 0;
                AutoScrollMinSize = new Size(1, 1 + linesCount + ClientRectangle.Height - linesPerPage);
                VerticalScroll.SmallChange = 1;
                VerticalScroll.LargeChange = ClientRectangle.Height / _charHeight;
                Invalidate();
            }
        }

        private void OnImageViewChange()
        {
            //UpdateImagePadding(_neighbourSizeMax);
            UpdateImagePadding();
        }

        public void UpdateImagePadding()
        //public void UpdateImagePadding(Size neighbourSizeMax)
        {
            //_neighbourSizeMax = neighbourSizeMax;
            //if (m_currentImageInfo != null)
            if (_currentImageWidth * _currentImageHeight > 0)
            {
                int horPos = 0, verPos = 0;
                int controlWidth = ClientSize.Width;
                int controlHeight = ClientSize.Height;
                int imageWidth = (int)_currentImageWidth;
                int imageHeigh = (int)_currentImageHeight;
                if (imageWidth > 0 && imageHeigh > 0)
                {
                    //if (imageWidth >= controlWidth || imageHeigh >= controlHeight)
                    {
                        if (imageWidth * controlHeight > imageHeigh * controlWidth)
                        {
                            verPos = (controlHeight - controlWidth * imageHeigh / imageWidth) / 2;
                        }
                        else
                        {
                            horPos = (controlWidth - controlHeight * imageWidth / imageHeigh) / 2;
                        }
                    }
                    /*else
                    {
                        verPos = (controlHeight - imageHeigh) / 2;
                        horPos = (controlWidth - imageWidth) / 2;
                    }*/
                }
                m_bitmapRect = new Rectangle(horPos, verPos, controlWidth - 2 * horPos, controlHeight - 2 * verPos);
            }
            Refresh();
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);
            Invalidate();
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            // 
            // FileViewer
            // 
            this.Name = "FileViewer";
            this.ResumeLayout(false);

        }

        /*protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _memoryStream.Dispose();
            m_bitmap.Dispose();
        }*/

    }
}
