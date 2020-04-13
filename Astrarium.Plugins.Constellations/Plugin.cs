﻿using Astrarium.Config;
using Astrarium.Types;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Astrarium.Plugins.Constellations
{
    public class Plugin : AbstractPlugin
    {
        public Plugin(ISettings settings)
        {
            #region Settings

            AddSetting(new SettingItem("ConstBorders", true, "Constellations"));
            AddSetting(new SettingItem("ConstLabels", true, "Constellations"));
            AddSetting(new SettingItem("ConstLabelsType", ConstellationsRenderer.LabelType.InternationalName, "Constellations", s => s.Get<bool>("ConstLabels")));
           
            AddSetting(new SettingItem("ColorConstBorders", Color.FromArgb(64, 32, 32), "Colors"));
            AddSetting(new SettingItem("ColorConstLabels", Color.FromArgb(64, 32, 32), "Colors"));
            
            #endregion Settings

            #region Toolbar Integration
            
            AddToolbarItem(new ToolbarToggleButton("IconConstBorders", "$Settings.ConstBorders", new SimpleBinding(settings, "ConstBorders", "IsChecked"), "Constellations"));
            AddToolbarItem(new ToolbarToggleButton("IconConstLabels", "$Settings.ConstLabels", new SimpleBinding(settings, "ConstLabels", "IsChecked"), "Constellations"));

            #endregion Toolbar Integration

            #region Exports

            ExportResourceDictionaries("Images.xaml");

            #endregion
        }
    }
}
