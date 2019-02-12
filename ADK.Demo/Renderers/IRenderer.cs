﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ADK.Demo.Renderers
{
    /// <summary>
    /// Defines methods for all renderer classes which implement drawing logic of sky map.
    /// </summary>
    public interface IRenderer
    {
        void Render(IMapContext map);
        void Initialize();
    }
}
