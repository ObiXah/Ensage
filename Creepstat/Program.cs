﻿
using System;
using System.Threading;
using Ensage;
using Ensage.Common;
using Ensage.Common.Menu;
using SharpDX;
using SharpDX.Direct3D9;

namespace Creepstat
{
    internal class Program
    {
        private static bool menuadded;
        private static Font _text;
        private static Font _notice;
        private static Line _line;
        private static Menu Menu;
        private static float startw, herow, spacew;
        private static Vector2 _screenSize;
        private static void Main()
        {
            Events.OnLoad += On_Load;
            Events.OnClose += On_Close;
            _text = new Font(
                Drawing.Direct3DDevice9,
                new FontDescription
                {
                    FaceName = "Segoe UI",
                    Height = 17,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.ClearType
                });

            _line = new Line(Drawing.Direct3DDevice9);
            Drawing.OnPreReset += Drawing_OnPreReset;
            Drawing.OnPostReset += Drawing_OnPostReset;
            Drawing.OnEndScene += Drawing_OnEndScene;
            AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;
            
        }

        private static void On_Load(object sender, EventArgs e)
        {
            if (!menuadded)
            {
                InitMenu();
                menuadded = true;
            }
        }

        private static void On_Close(object sender, EventArgs e)
        {
            if (menuadded) Menu.RemoveFromMainMenu();
            menuadded = false;
        }

        private static void InitMenu()
        {
            Menu = new Menu("Creepstat", "csbb", true);
            Menu.AddItem(
                new MenuItem("cs", "Creepstat").SetValue(true));
            Menu.AddItem(
                new MenuItem("bb", "Buyback").SetValue(true));
            Menu.AddToMainMenu();
        }

        private static void CurrentDomain_DomainUnload(object sender, EventArgs e)
        {
            _text.Dispose();
            _notice.Dispose();
            _line.Dispose();
        }

        private static void Drawing_OnEndScene(EventArgs args)
        {
            if (!menuadded) return;
            if (Drawing.Direct3DDevice9 == null || Drawing.Direct3DDevice9.IsDisposed || !Game.IsInGame)
                return;
            var player = ObjectMgr.LocalPlayer;
            if (player == null || player.Team == Team.Observer)
                return;

            if (startw == 0)
            {
                _screenSize = new Vector2(Drawing.Width, Drawing.Height);

                startw = 520f / 1920f * _screenSize.X;
                spacew = (208f / 1920f) * _screenSize.X;
                herow = (66f / 1920f) * _screenSize.X;
                Console.WriteLine(_screenSize.X.ToString() + startw.ToString());
                //startw = 520;
                //spacew = 208;
                //herow = 66;
            }

             if (Menu.Item("cs").GetValue<bool>() || Menu.Item("bb").GetValue<bool>())
            {
                for (uint i = 0; i < 10; i++)
                {
                    Player p = null;
                    try
                    {
                        p = ObjectMgr.GetPlayerById(i);
                    }
                    catch
                    {
                    }
                    if (p == null) continue;

                    var initPos = (int)(i >= 5
                        ? (startw + herow * i) + spacew
                        : (startw + herow * i));
                    if (Menu.Item("cs").GetValue<bool>())
                    {
                        var text = string.Format("{0}/{1}", p.LastHitCount, p.DenyCount);
                        DrawShadowText(text, initPos + 10, 35 + 1 - 7 * 5, Color.White, _text);
                    }
                    if (Menu.Item("bb").GetValue<bool>() && p.BuybackCooldownTime > 0)
                    {
                        var text = string.Format("{0:0.}", p.BuybackCooldownTime);
                        DrawFilledBox(initPos + 2, 35 + 1 - 7 * 5 + 22, 35, 15, new Color(0, 0, 0, 150));
                        DrawShadowText(text, initPos + 5, 35 + 1 - 7 * 5 + 22, Color.White, _text);
                    }
                }
            }
        }

        private static void Drawing_OnPostReset(EventArgs args)
        {
            _text.OnResetDevice();
            _notice.OnResetDevice();
            _line.OnResetDevice();
        }

        private static void Drawing_OnPreReset(EventArgs args)
        {
            _text.OnLostDevice();
            _notice.OnLostDevice();
            _line.OnLostDevice();
        }

        public static void DrawFilledBox(float x, float y, float w, float h, Color color)
        {
            var vLine = new Vector2[2];

            _line.GLLines = true;
            _line.Antialias = false;
            _line.Width = w;

            vLine[0].X = x + w/2;
            vLine[0].Y = y;
            vLine[1].X = x + w/2;
            vLine[1].Y = y + h;

            _line.Begin();
            _line.Draw(vLine, color);
            _line.End();
        }

        public static void DrawBox(float x, float y, float w, float h, float px, Color color)
        {
            DrawFilledBox(x, y + h, w, px, color);
            DrawFilledBox(x - px, y, px, h, color);
            DrawFilledBox(x, y - px, w, px, color);
            DrawFilledBox(x + w, y, px, h, color);
        }

        public static void DrawShadowText(string stext, int x, int y, Color color, Font f)
        {
            f.DrawText(null, stext, x + 1, y + 1, Color.Black);
            f.DrawText(null, stext, x, y, color);
        }
    }
}


