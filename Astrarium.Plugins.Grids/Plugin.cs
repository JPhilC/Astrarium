﻿using Astrarium.Config;
using Astrarium.Types;
using Astrarium.Types.Config.Controls;
using Astrarium.Types.Localization;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Astrarium.Plugins.Grids
{
    public class Plugin : AbstractPlugin
    {
        public Plugin(ISettings settings)
        {
            SettingItems.Add("Grids", new SettingItem("EquatorialGrid", false, "Grids"));
            SettingItems.Add("Grids", new SettingItem("LabelEquatorialPoles", true, "Grids", s => s.Get<bool>("EquatorialGrid")));
            SettingItems.Add("Grids", new SettingItem("HorizontalGrid", false, "Grids"));
            SettingItems.Add("Grids", new SettingItem("LabelHorizontalPoles", true, "Grids", s => s.Get<bool>("HorizontalGrid")));
            SettingItems.Add("Grids", new SettingItem("EclipticLine", true, "Grids"));
            SettingItems.Add("Grids", new SettingItem("LabelEquinoxPoints", false, "Grids", s => s.Get<bool>("EclipticLine")));
            SettingItems.Add("Grids", new SettingItem("LabelLunarNodes", false, "Grids", s => s.Get<bool>("EclipticLine")));
            SettingItems.Add("Grids", new SettingItem("GalacticEquator", true, "Grids"));

            SettingItems.Add("Colors", new SettingItem("ColorEcliptic", Color.FromArgb(0xC8, 0x80, 0x80, 0x00), "Colors"));
            SettingItems.Add("Colors", new SettingItem("ColorGalacticEquator", Color.FromArgb(200, 64, 0, 64), "Colors"));
            SettingItems.Add("Colors", new SettingItem("ColorHorizontalGrid", Color.FromArgb(0xC8, 0x00, 0x40, 0x00), "Colors"));
            SettingItems.Add("Colors", new SettingItem("ColorEquatorialGrid", Color.FromArgb(200, 0, 64, 64), "Colors"));

            ToolbarItems.Add("Grids", new ToolbarToggleButton("IconEquatorialGrid", "$Settings.EquatorialGrid", new SimpleBinding(settings, "EquatorialGrid", "IsChecked")));
            ToolbarItems.Add("Grids", new ToolbarToggleButton("IconHorizontalGrid", "$Settings.HorizontalGrid", new SimpleBinding(settings, "HorizontalGrid", "IsChecked")));

            ExportResourceDictionaries("Images.xaml");
        }
    }
}