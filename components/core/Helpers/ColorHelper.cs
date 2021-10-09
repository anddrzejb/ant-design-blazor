﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace AntDesign
{
    internal static class ColorHelper
    {
        public static string GetBackgroundStyle(Color color) => $"background-color: {_basicColors[color]}; border-color: {_basicColors[color]}; color: {_foreColors[color]};";
        public static string GetColor(Color color) => _basicColors[color];

        private static Dictionary<Color, string> _basicColors = new Dictionary<Color, string>()
        {
            { Color.None, "" },
            { Color.Red1, "#fff1f0" },
            { Color.Red2, "#ffccc7" },
            { Color.Red3, "#ffa39e" },
            { Color.Red4, "#ff7875" },
            { Color.Red5, "#ff4d4f" },
            { Color.Red6, "#f5222d" },
            { Color.Red7, "#cf1322" },
            { Color.Red8, "#a8071a" },
            { Color.Red9, "#820014" },
            { Color.Red10, "#5c0011" },
            { Color.Volcano1, "#fff2e8" },
            { Color.Volcano2, "#ffd8bf" },
            { Color.Volcano3, "#ffbb96" },
            { Color.Volcano4, "#ff9c6e" },
            { Color.Volcano5, "#ff7a45" },
            { Color.Volcano6, "#fa541c" },
            { Color.Volcano7, "#d4380d" },
            { Color.Volcano8, "#ad2102" },
            { Color.Volcano9, "#871400" },
            { Color.Volcano10, "#610b00" },
            { Color.Orange1, "#fff7e6" },
            { Color.Orange2, "#ffe7ba" },
            { Color.Orange3, "#ffd591" },
            { Color.Orange4, "#ffc069" },
            { Color.Orange5, "#ffa940" },
            { Color.Orange6, "#fa8c16" },
            { Color.Orange7, "#d46b08" },
            { Color.Orange8, "#ad4e00" },
            { Color.Orange9, "#873800" },
            { Color.Orange10, "#612500" },
            { Color.Gold1, "#fffbe6" },
            { Color.Gold2, "#fff1b8" },
            { Color.Gold3, "#ffe58f" },
            { Color.Gold4, "#ffd666" },
            { Color.Gold5, "#ffc53d" },
            { Color.Gold6, "#faad14" },
            { Color.Gold7, "#d48806" },
            { Color.Gold8, "#ad6800" },
            { Color.Gold9, "#874d00" },
            { Color.Gold10, "#613400" },
            { Color.Yellow1, "#feffe6" },
            { Color.Yellow2, "#ffffb8" },
            { Color.Yellow3, "#fffb8f" },
            { Color.Yellow4, "#fff566" },
            { Color.Yellow5, "#ffec3d" },
            { Color.Yellow6, "#fadb14" },
            { Color.Yellow7, "#d4b106" },
            { Color.Yellow8, "#ad8b00" },
            { Color.Yellow9, "#876800" },
            { Color.Yellow10, "#614700" },
            { Color.Lime1, "#fcffe6" },
            { Color.Lime2, "#f4ffb8" },
            { Color.Lime3, "#eaff8f" },
            { Color.Lime4, "#d3f261" },
            { Color.Lime5, "#bae637" },
            { Color.Lime6, "#a0d911" },
            { Color.Lime7, "#7cb305" },
            { Color.Lime8, "#5b8c00" },
            { Color.Lime9, "#3f6600" },
            { Color.Lime10, "#254000" },
            { Color.Green1, "#f6ffed" },
            { Color.Green2, "#d9f7be" },
            { Color.Green3, "#b7eb8f" },
            { Color.Green4, "#95de64" },
            { Color.Green5, "#73d13d" },
            { Color.Green6, "#52c41a" },
            { Color.Green7, "#389e0d" },
            { Color.Green8, "#237804" },
            { Color.Green9, "#135200" },
            { Color.Green10, "#092b00" },
            { Color.Cyan1, "#e6fffb" },
            { Color.Cyan2, "#b5f5ec" },
            { Color.Cyan3, "#87e8de" },
            { Color.Cyan4, "#5cdbd3" },
            { Color.Cyan5, "#36cfc9" },
            { Color.Cyan6, "#13c2c2" },
            { Color.Cyan7, "#08979c" },
            { Color.Cyan8, "#006d75" },
            { Color.Cyan9, "#00474f" },
            { Color.Cyan10, "#002329" },
            { Color.Blue1, "#e6f7ff" },
            { Color.Blue2, "#bae7ff" },
            { Color.Blue3, "#91d5ff" },
            { Color.Blue4, "#69c0ff" },
            { Color.Blue5, "#40a9ff" },
            { Color.Blue6, "#1890ff" },
            { Color.Blue7, "#096dd9" },
            { Color.Blue8, "#0050b3" },
            { Color.Blue9, "#003a8c" },
            { Color.Blue10, "#002766" },
            { Color.Geekblue1, "#f0f5ff" },
            { Color.Geekblue2, "#d6e4ff" },
            { Color.Geekblue3, "#adc6ff" },
            { Color.Geekblue4, "#85a5ff" },
            { Color.Geekblue5, "#597ef7" },
            { Color.Geekblue6, "#2f54eb" },
            { Color.Geekblue7, "#1d39c4" },
            { Color.Geekblue8, "#10239e" },
            { Color.Geekblue9, "#061178" },
            { Color.Geekblue10, "#030852" },
            { Color.Purple1, "#f9f0ff" },
            { Color.Purple2, "#efdbff" },
            { Color.Purple3, "#d3adf7" },
            { Color.Purple4, "#b37feb" },
            { Color.Purple5, "#9254de" },
            { Color.Purple6, "#722ed1" },
            { Color.Purple7, "#531dab" },
            { Color.Purple8, "#391085" },
            { Color.Purple9, "#22075e" },
            { Color.Purple10, "#120338" },
            { Color.Magenta1, "#fff0f6" },
            { Color.Magenta2, "#ffd6e7" },
            { Color.Magenta3, "#ffadd2" },
            { Color.Magenta4, "#ff85c0" },
            { Color.Magenta5, "#f759ab" },
            { Color.Magenta6, "#eb2f96" },
            { Color.Magenta7, "#c41d7f" },
            { Color.Magenta8, "#9e1068" },
            { Color.Magenta9, "#780650" },
            { Color.Magenta10, "#520339" },
            { Color.Gray1, "#ffffff" },
            { Color.Gray2, "#fafafa" },
            { Color.Gray3, "#f5f5f5" },
            { Color.Gray4, "#f0f0f0" },
            { Color.Gray5, "#d9d9d9" },
            { Color.Gray6, "#bfbfbf" },
            { Color.Gray7, "#8c8c8c" },
            { Color.Gray8, "#595959" },
            { Color.Gray9, "#434343" },
            { Color.Gray10, "#262626" },
            { Color.Gray11, "#1f1f1f" },
            { Color.Gray12, "#141414" },
            { Color.Gray13, "#000000" },
        };

        private static Dictionary<Color, string> _foreColors = new Dictionary<Color, string>()
        {
            { Color.None, "" },
            { Color.Red1, "rgba(0,0,0,0.85);" },
            { Color.Red2, "rgba(0,0,0,0.85);" },
            { Color.Red3, "rgba(0,0,0,0.85);" },
            { Color.Red4, "rgba(0,0,0,0.85);" },
            { Color.Red5, "rgba(0,0,0,0.85);" },
            { Color.Red6, "rgb(255,255,255);" },
            { Color.Red7, "rgb(255,255,255);" },
            { Color.Red8, "rgb(255,255,255);" },
            { Color.Red9, "rgb(255,255,255);" },
            { Color.Red10, "rgb(255,255,255);" },
            { Color.Volcano1, "rgba(0,0,0,0.85);" },
            { Color.Volcano2, "rgba(0,0,0,0.85);" },
            { Color.Volcano3, "rgba(0,0,0,0.85);" },
            { Color.Volcano4, "rgba(0,0,0,0.85);" },
            { Color.Volcano5, "rgba(0,0,0,0.85);" },
            { Color.Volcano6, "rgb(255,255,255);" },
            { Color.Volcano7, "rgb(255,255,255);" },
            { Color.Volcano8, "rgb(255,255,255);" },
            { Color.Volcano9, "rgb(255,255,255);" },
            { Color.Volcano10, "rgb(255,255,255);" },
            { Color.Orange1, "rgba(0,0,0,0.85);" },
            { Color.Orange2, "rgba(0,0,0,0.85);" },
            { Color.Orange3, "rgba(0,0,0,0.85);" },
            { Color.Orange4, "rgba(0,0,0,0.85);" },
            { Color.Orange5, "rgba(0,0,0,0.85);" },
            { Color.Orange6, "rgb(255,255,255);" },
            { Color.Orange7, "rgb(255,255,255);" },
            { Color.Orange8, "rgb(255,255,255);" },
            { Color.Orange9, "rgb(255,255,255);" },
            { Color.Orange10, "rgb(255,255,255);" },
            { Color.Gold1, "rgba(0,0,0,0.85);" },
            { Color.Gold2, "rgba(0,0,0,0.85);" },
            { Color.Gold3, "rgba(0,0,0,0.85);" },
            { Color.Gold4, "rgba(0,0,0,0.85);" },
            { Color.Gold5, "rgba(0,0,0,0.85);" },
            { Color.Gold6, "rgb(255,255,255);" },
            { Color.Gold7, "rgb(255,255,255);" },
            { Color.Gold8, "rgb(255,255,255);" },
            { Color.Gold9, "rgb(255,255,255);" },
            { Color.Gold10, "rgb(255,255,255);" },
            { Color.Yellow1, "rgba(0,0,0,0.85);" },
            { Color.Yellow2, "rgba(0,0,0,0.85);" },
            { Color.Yellow3, "rgba(0,0,0,0.85);" },
            { Color.Yellow4, "rgba(0,0,0,0.85);" },
            { Color.Yellow5, "rgba(0,0,0,0.85);" },
            { Color.Yellow6, "rgba(0,0,0,0.85);" },
            { Color.Yellow7, "rgb(255,255,255);" },
            { Color.Yellow8, "rgb(255,255,255);" },
            { Color.Yellow9, "rgb(255,255,255);" },
            { Color.Yellow10, "rgb(255,255,255);" },
            { Color.Lime1, "rgba(0,0,0,0.85);" },
            { Color.Lime2, "rgba(0,0,0,0.85);" },
            { Color.Lime3, "rgba(0,0,0,0.85);" },
            { Color.Lime4, "rgba(0,0,0,0.85);" },
            { Color.Lime5, "rgba(0,0,0,0.85);" },
            { Color.Lime6, "rgb(255,255,255);" },
            { Color.Lime7, "rgb(255,255,255);" },
            { Color.Lime8, "rgb(255,255,255);" },
            { Color.Lime9, "rgb(255,255,255);" },
            { Color.Lime10, "rgb(255,255,255);" },
            { Color.Green1, "rgba(0,0,0,0.85);" },
            { Color.Green2, "rgba(0,0,0,0.85);" },
            { Color.Green3, "rgba(0,0,0,0.85);" },
            { Color.Green4, "rgba(0,0,0,0.85);" },
            { Color.Green5, "rgba(0,0,0,0.85);" },
            { Color.Green6, "rgb(255,255,255);" },
            { Color.Green7, "rgb(255,255,255);" },
            { Color.Green8, "rgb(255,255,255);" },
            { Color.Green9, "rgb(255,255,255);" },
            { Color.Green10, "rgb(255,255,255);" },
            { Color.Cyan1, "rgba(0,0,0,0.85);" },
            { Color.Cyan2, "rgba(0,0,0,0.85);" },
            { Color.Cyan3, "rgba(0,0,0,0.85);" },
            { Color.Cyan4, "rgba(0,0,0,0.85);" },
            { Color.Cyan5, "rgba(0,0,0,0.85);" },
            { Color.Cyan6, "rgb(255,255,255);" },
            { Color.Cyan7, "rgb(255,255,255);" },
            { Color.Cyan8, "rgb(255,255,255);" },
            { Color.Cyan9, "rgb(255,255,255);" },
            { Color.Cyan10, "rgb(255,255,255);" },
            { Color.Blue1, "rgba(0,0,0,0.85);" },
            { Color.Blue2, "rgba(0,0,0,0.85);" },
            { Color.Blue3, "rgba(0,0,0,0.85);" },
            { Color.Blue4, "rgba(0,0,0,0.85);" },
            { Color.Blue5, "rgba(0,0,0,0.85);" },
            { Color.Blue6, "rgb(255,255,255);" },
            { Color.Blue7, "rgb(255,255,255);" },
            { Color.Blue8, "rgb(255,255,255);" },
            { Color.Blue9, "rgb(255,255,255);" },
            { Color.Blue10, "rgb(255,255,255);" },
            { Color.Geekblue1, "rgba(0,0,0,0.85);" },
            { Color.Geekblue2, "rgba(0,0,0,0.85);" },
            { Color.Geekblue3, "rgba(0,0,0,0.85);" },
            { Color.Geekblue4, "rgba(0,0,0,0.85);" },
            { Color.Geekblue5, "rgba(0,0,0,0.85);" },
            { Color.Geekblue6, "rgb(255,255,255);" },
            { Color.Geekblue7, "rgb(255,255,255);" },
            { Color.Geekblue8, "rgb(255,255,255);" },
            { Color.Geekblue9, "rgb(255,255,255);" },
            { Color.Geekblue10, "rgb(255,255,255);" },
            { Color.Purple1, "rgba(0,0,0,0.85);" },
            { Color.Purple2, "rgba(0,0,0,0.85);" },
            { Color.Purple3, "rgba(0,0,0,0.85);" },
            { Color.Purple4, "rgba(0,0,0,0.85);" },
            { Color.Purple5, "rgba(0,0,0,0.85);" },
            { Color.Purple6, "rgb(255,255,255);" },
            { Color.Purple7, "rgb(255,255,255);" },
            { Color.Purple8, "rgb(255,255,255);" },
            { Color.Purple9, "rgb(255,255,255);" },
            { Color.Purple10, "rgb(255,255,255);" },
            { Color.Magenta1, "rgba(0,0,0,0.85);" },
            { Color.Magenta2, "rgba(0,0,0,0.85);" },
            { Color.Magenta3, "rgba(0,0,0,0.85);" },
            { Color.Magenta4, "rgba(0,0,0,0.85);" },
            { Color.Magenta5, "rgba(0,0,0,0.85);" },
            { Color.Magenta6, "rgb(255,255,255);" },
            { Color.Magenta7, "rgb(255,255,255);" },
            { Color.Magenta8, "rgb(255,255,255);" },
            { Color.Magenta9, "rgb(255,255,255);" },
            { Color.Magenta10, "rgb(255,255,255);" },
            { Color.Gray1, "rgba(0,0,0,0.85);" },
            { Color.Gray2, "rgba(0,0,0,0.85);" },
            { Color.Gray3, "rgba(0,0,0,0.85);" },
            { Color.Gray4, "rgba(0,0,0,0.85);" },
            { Color.Gray5, "rgba(0,0,0,0.85);" },
            { Color.Gray6, "rgb(255,255,255);" },
            { Color.Gray7, "rgb(255,255,255);" },
            { Color.Gray8, "rgb(255,255,255);" },
            { Color.Gray9, "rgb(255,255,255);" },
            { Color.Gray10, "rgb(255,255,255);" },
            { Color.Gray11, "rgb(255,255,255);" },
            { Color.Gray12, "rgb(255,255,255);" },
            { Color.Gray13, "rgb(255,255,255);" }, 
        };
    }
}
