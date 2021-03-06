﻿using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using UVA_Arena;

namespace System.Windows.Forms
{
    public class CustomTabControl : TabControl
    {
        private System.ComponentModel.IContainer components = null;

        //
        // Constructor and Properties
        //
        public CustomTabControl()
        {
            components = new System.ComponentModel.Container();
            
            this.BackColor = Color.PaleTurquoise;

            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.EnableNotifyMessage, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);            
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);            
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        [Category("Appearance"), DefaultValue(0)]
        public int Overlap { get; set; }

        [Category("Appearance"), DefaultValue(typeof(Color), "PaleTurquoise"), EditorBrowsable(EditorBrowsableState.Always)]
        new public Color BackColor { get; set; }

        //
        // Events
        //

        protected override void OnSelected(TabControlEventArgs e)
        {
            base.OnSelected(e);
            this.Invalidate();
        }
        
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            //draw on buffer image first to reduce flickering
            Bitmap BufferImage = new Bitmap(this.Width, this.Height);
            Graphics graphics = Graphics.FromImage(BufferImage);

            graphics.SmoothingMode = SmoothingMode.HighQuality;

            //paint background with back color
            graphics.Clear(this.BackColor);

            //draw tab buttons
            if (this.TabCount > 0)
            {
                //currently selected tab button should be drawn later 
                // so that it can appear on top
                for (int index = this.TabCount - 1; index >= 0; --index)
                {
                    if (index != this.SelectedIndex)
                    {
                        DrawTabPage(index, graphics);
                    }
                }
                if (this.SelectedIndex != -1)
                {
                    DrawTabPage(this.SelectedIndex, graphics);
                }
            }

            //draw buffer image on screen
            graphics.Flush();
            e.Graphics.DrawImageUnscaled(BufferImage, 0, 0);

            //dispose
            graphics.Dispose();
            BufferImage.Dispose();
        }

        //
        // Draw TabPage
        //
        private void DrawTabPage(int index, Graphics graphics)
        {
            //gets if the current tab page has mouse focus
            bool mouseFocus = false;
            Rectangle mouse = new Rectangle(MousePosition.X, MousePosition.Y, 1, 1);
            if (RectangleToScreen(GetTabRect(index)).IntersectsWith(mouse))
            {
                mouseFocus = true;
            }

            //get brush for tab button 
            Pen borderPen = null;
            Brush fillbrush = null;
            if (this.SelectedIndex == index)
            {
                borderPen = new Pen(Color.FromArgb(147, 177, 205));
                Color fore = Color.FromArgb(215, 210, 230);
                Color back = Color.FromArgb(235, 220, 240);
                Stylish.GradientStyle style = new Stylish.GradientStyle(HatchStyle.DottedGrid, fore, back);
                fillbrush = style.GetBrush();
            }
            else
            {
                borderPen = new Pen(Color.FromArgb(167, 197, 235));
                Color up = Color.FromArgb(215, 208, 230);
                Color down = Color.FromArgb(246, 242, 252);
                if(mouseFocus)
                {
                    up = Color.FromArgb(225, 219, 242);
                    down = Color.FromArgb(236, 238, 248);
                }
                Stylish.GradientStyle style = new Stylish.GradientStyle(up, down, 90F);
                Rectangle tabBounds = GetTabRectAdjusted(index);
                fillbrush = style.GetBrush(tabBounds.Width, tabBounds.Height + 1);
            }

            //draw tab button back color
            GraphicsPath path = GetTabPageBorder(index);
            graphics.SmoothingMode = SmoothingMode.HighQuality;

            //draw tab button and text and image on it 
            graphics.FillPath(fillbrush, path); //tab button
            this.DrawTabText(index, graphics); //tab text
            this.DrawTabImage(index, graphics); //tab image
            graphics.DrawPath(borderPen, path); // tab border

            path.Dispose();
            fillbrush.Dispose();
            borderPen.Dispose();
        }

        Rectangle GetTabRectAdjusted(int index)
        {
            Rectangle tabBounds = this.GetTabRect(index);
            tabBounds.Height += 1;
            if (index == 0)
            {
                tabBounds.X += 1;
                tabBounds.Width -= 1;
            }
            else
            {
                tabBounds.X -= this.Overlap;
                tabBounds.Width += this.Overlap;
            }

            return tabBounds;
        }

        GraphicsPath GetTabPageBorder(int index)
        {
            GraphicsPath path = new GraphicsPath();

            Rectangle tabBounds = this.GetTabRectAdjusted(index);
            Rectangle pageBounds = this.TabPages[index].Bounds;
            pageBounds.X -= 1;
            pageBounds.Y -= 1;
            pageBounds.Width += 3;
            pageBounds.Height += 2;

            //--> google chrome style tab border
            // bottom left of tab up the leading slope
            path.AddLine(tabBounds.X, tabBounds.Bottom, tabBounds.X + tabBounds.Height - 4, tabBounds.Y + 2);
            // along the top, leaving a gap that is auto completed for us
            path.AddLine(tabBounds.X + tabBounds.Height, tabBounds.Y, tabBounds.Right - 3, tabBounds.Y);
            // round the top right corner
            path.AddArc(tabBounds.Right - 6, tabBounds.Y, 6, 6, 270, 90);
            // back down the right end
            path.AddLine(tabBounds.Right, tabBounds.Y + 3, tabBounds.Right, tabBounds.Bottom);
            // no need to complete the figure as this path joins into the border for the entire tab
            /* Courtesy : http://www.codeproject.com/Articles/91387/Painting-Your-Own-Tabs-Second-Edition */

            //add page border
            path.AddRectangle(pageBounds);

            path.CloseFigure();
            return path;
        }

        private void DrawTabText(int index, Graphics graphics)
        {
            Rectangle tabBounds = this.GetTabRect(index);

            tabBounds.X += 16 + this.Padding.X;
            tabBounds.Width -= 16 + this.Padding.X;
            tabBounds.Y += (int)(tabBounds.Height - this.Font.Height) / 2;

            if (this.ImageList != null && (this.TabPages[index].ImageIndex != -1
                || !String.IsNullOrEmpty(this.TabPages[index].ImageKey)))
            {
                int extra = (int)(this.ImageList.ImageSize.Width * 1.5);
                tabBounds.X += extra;
                tabBounds.Width -= extra;
            }

            Rectangle tabBounds2 = tabBounds;
            tabBounds2.X += 1;
            tabBounds2.Y += 1;

            Rectangle tabBounds3 = tabBounds;
            tabBounds3.X -= 1;
            tabBounds3.Y -= 1;

            graphics.DrawString(this.TabPages[index].Text, this.Font, Brushes.LightBlue, tabBounds2); //shadow
            graphics.DrawString(this.TabPages[index].Text, this.Font, Brushes.LightBlue, tabBounds3); //shadow
            graphics.DrawString(this.TabPages[index].Text, this.Font, Brushes.Black, tabBounds); //main
        }

        private void DrawTabImage(int index, Graphics graphics)
        {
            if (this.ImageList == null) return;

            Image tabImage = null;
            int imgidx = this.TabPages[index].ImageIndex;
            String imgkey = this.TabPages[index].ImageKey;

            if (imgidx >= 0) tabImage = this.ImageList.Images[imgidx];
            else if (!string.IsNullOrEmpty(imgkey)) tabImage = this.ImageList.Images[imgkey];

            if (tabImage == null) return;

            Rectangle imgRect = this.GetTabRect(index);
            imgRect.X += 20 + this.Padding.X;
            imgRect.Y = imgRect.Y + (imgRect.Height - tabImage.Height) / 2;
            imgRect.Width = tabImage.Width;
            imgRect.Height = tabImage.Height;

            graphics.DrawImageUnscaled(tabImage, imgRect);
            tabImage.Dispose();
        }
    }
}