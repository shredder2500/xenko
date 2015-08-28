﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SiliconStudio.Assets;
using SiliconStudio.Core;

namespace SiliconStudio.Paradox.Assets.Scripts
{
    [DataContract("EffectCompositorAsset")]
    [AssetDescription(".cs")]
    [Display(95, "Script Source Code", "A script source code file")]
    public sealed class ScriptSourceFileAsset : SourceCodeAsset
    {
        public ScriptSourceFileAsset()
        {
            Debug.WriteLine("x");
        }
    }
}
