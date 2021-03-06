﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using XHTMLClassLibrary.BaseElements.InlineElements;

namespace XHTMLClassLibrary.BaseElements.BlockElements
{
    /// <summary>
    /// The "aside" tag defines some content aside from the content it is placed in.
    /// The aside content should be related to the surrounding content.
    /// </summary>
    [HTMLItemAttribute(ElementName = "aside", SupportedStandards = HTMLElementType.HTML5)]
    public class Aside : HTMLItem, IBlockElement
    {
        public Aside(HTMLElementType htmlStandard) : base(htmlStandard)
        {
        }

        public override bool IsValid()
        {
            return Subitems.All(item => item.IsValid());
        }

        protected override bool IsValidSubType(IHTMLItem item)
        {
            if (item is IInlineItem ||
                item is IBlockElement ||
                item is SimpleHTML5Text)
            {
                return item.IsValid();
            }
            return false;
        }


    }
}
