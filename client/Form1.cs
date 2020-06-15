﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows;
using System.IO;

using MyUtil;   //自作dllを使用するため

namespace client
{
	public partial class Form1 : Form
	{
		Bitmap pintedImg_;
		Pen pen_white;
        Pen pen_red;
        Pen pen_black;
        Pen pen_blue;
        Pen pen_green;
        Pen pen_yellow;
        HandleClient handle_;

		public Form1()
		{
			InitializeComponent();
            pen_red = new Pen(Color.FromArgb(255, 255, 0, 0), 5);
            pen_black = new Pen(Color.FromArgb(255, 0, 0, 0), 5);
            pen_blue = new Pen(Color.FromArgb(255, 0, 120, 255), 5);
            pen_green = new Pen(Color.FromArgb(255, 21, 184, 0), 5);
            pen_yellow = new Pen(Color.FromArgb(255, 255, 216, 0), 5);
            pen_white = new Pen(Color.FromArgb(255, 255, 255, 255), 30);
        }

		private void btnConnect_Click(object sender, EventArgs e)
		{
            if(textName.Text == "")
            {
                MessageBox.Show("名前を入力してください");
                return;
            }

			int port = 0;
			bool canConvert = int.TryParse(textPort.Text, out port);
			if (canConvert == false)
			{
				MessageBox.Show("ポート番号に数値を入力してください");
				return;
			}

			handle_ = new HandleClient();
			if (handle_.start(textName.Text, textIP.Text, port))
            {
				pintedImg_ = new Bitmap(pictureBox1.Width, pictureBox1.Height);

				handle_.PaintEvent = (PaintInfo info) =>
				{
					if (this.InvokeRequired)//別スレッドから呼び出されたとき Invokeして呼びなおす
					{
						this.Invoke(new Action(() => handle_.PaintEvent(info)), null);
						return;
					}
					using (var g = Graphics.FromImage(pintedImg_))
					{
                        Point fillPos = info.StartPos; fillPos.X = -1; fillPos.Y = -1;
                        if (fillPos == info.StartPos && fillPos == info.EndPos)
                        {
                            g.FillRectangle(Brushes.White, 50, 15, 520, 395);
                            pictureBox1.Image = pintedImg_; //描画更新
                        }
                        else
                        {
                            switch (info.Pen_color)
                            {
                                case 1:
                                    g.DrawLine(pen_black, info.StartPos.X, info.StartPos.Y, info.EndPos.X, info.EndPos.Y);
                                    break;
                                case 2:
                                    g.DrawLine(pen_red, info.StartPos.X, info.StartPos.Y, info.EndPos.X, info.EndPos.Y);
                                    break;
                                case 3:
                                    g.DrawLine(pen_blue, info.StartPos.X, info.StartPos.Y, info.EndPos.X, info.EndPos.Y);
                                    break;
                                case 4:
                                    g.DrawLine(pen_yellow, info.StartPos.X, info.StartPos.Y, info.EndPos.X, info.EndPos.Y);
                                    break;
                                case 5:
                                    g.DrawLine(pen_green, info.StartPos.X, info.StartPos.Y, info.EndPos.X, info.EndPos.Y);
                                    break;
                                case 6:
                                    g.DrawLine(pen_white, info.StartPos.X, info.StartPos.Y, info.EndPos.X, info.EndPos.Y);
                                    break;
                            }
                            pictureBox1.Image = pintedImg_; //描画更新					
                        }
                    }
				};

				handle_.RecivedMessageEvent = (string name, string text) =>
				{
					if (this.InvokeRequired)//別スレッドから呼び出されたとき Invokeして呼びなおす
					{
						this.Invoke(new Action(() => handle_.RecivedMessageEvent(name, text)), null);
						return;
					}

					textInfo.Text += (name + " >> " + text + "\r\n");
                    textInfo.HideSelection = false;
                    textInfo.AppendText("\n");
                };

                handle_.RecivedImageEvent = (string name, byte[] image) =>
                {
                    if (this.InvokeRequired)//別スレッドから呼び出されたとき Invokeして呼びなおす
                    {
                        this.Invoke(new Action(() => handle_.RecivedImageEvent(name, image)), null);
                        return;
                    }
                    pictureBox2.SizeMode = PictureBoxSizeMode.Zoom;
                    Image img = Util.ByteArrayToImage(image);
                    pictureBox2.Image = img;
                };

                labelState.Text = "connected";
                btnConnect.Enabled = false;
            }
        }

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (handle_ != null)
			{
				handle_.IsWorking = false;
			}
		}

		//チャット送信ボタン
		private void btnMessageSend_Click(object sender, EventArgs e)
		{
			if(handle_ == null || !handle_.IsWorking)
			{
				MessageBox.Show("サーバと通信していません");
				return;
			}
			handle_.sendMessage(textSendMessage.Text);
			handle_.RecivedMessageEvent("自分", textSendMessage.Text);
			textSendMessage.Text = "";
		}

		//チャットのテキストボックス内でEnterをおしたら送信ボタンを押したことにする
		private void textSendMessage_KeyDown(object sender, KeyEventArgs e)
		{
			if(e.KeyCode == Keys.Enter)
			{
				btnMessageSend.PerformClick();
			}
        }

        private void ImageButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "All Files(*.*)|*.*|Png File|*.png|Bmp File|*.bmp";
            openFileDialog1.FileName = "";
            openFileDialog1.Title = "送信するイメージ画像を選択してください";

            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var FileName = openFileDialog1.FileName;
                var ext = Path.GetExtension(FileName);
                if (ext != ".png" &&
                    ext != ".PNG" &&
                    ext != ".bmp" &&
                    ext != ".jpg" && ext != ".jpeg" &&
                    ext != ".JPG" && ext != ".JPEG")
                {
                    return;
                }
                try
                {
                    pictureBox2.SizeMode = PictureBoxSizeMode.Zoom;
                    using (var openFile = new FileStream(openFileDialog1.FileName, FileMode.Open, FileAccess.Read))
                    {
                        byte[] bs = new byte[openFile.Length];
                        openFile.Read(bs, 0, bs.Length);
                        ImageConverter imgconv = new ImageConverter();
                        Image img = (Image)imgconv.ConvertFrom(bs);
                        pictureBox2.Image = img;
                        handle_.sendImage(bs);
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("ファイルが開けませんでした", "読み込み失敗", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

            }
        }
    }
}
