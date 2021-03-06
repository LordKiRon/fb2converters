﻿using System.Xml.Linq;

namespace EPubLibraryContracts
{
    /// <summary>
    /// Contains different namespaces used by epub
    /// </summary>
    public static class EPubNamespaces
    {
        /// <summary>
        /// OPF namespace 
        /// http://www.idpf.org/2007/opf
        /// </summary>
        public static readonly XNamespace OpfNameSpace = @"http://www.idpf.org/2007/opf";

        /// <summary>
        /// Temporary namespace placeholer
        /// http://www.idpf.org/2007/xxx
        /// </summary>
        public  static readonly XNamespace FakeOpf = @"http://www.idpf.org/2007/xxx";

        /// <summary>
        /// OPS Namespace
        /// http://www.idpf.org/2007/ops
        /// </summary>
        public static readonly XNamespace OpsNamespace = @"http://www.idpf.org/2007/ops";
    }

    /// <summary>
    /// PURL namespaces
    /// </summary>
    public static class PURLNamespaces
    {
        /// <summary>
        /// DC namespace 
        /// http://purl.org/dc/elements/1.1/
        /// </summary>
        public static readonly XNamespace DCElements = @"http://purl.org/dc/elements/1.1/";

        /// <summary>
        /// DC Terms namespace
        /// http://purl.org/dc/terms/
        /// </summary>
        public static readonly XNamespace DCTerms = @"http://purl.org/dc/terms/";
    }

    /// <summary>
    /// WWW (w3) namespaces
    /// </summary>
    public static class WWWNamespaces
    {
        /// <summary>
        /// XSI namespace
        /// http://www.w3.org/2001/XMLSchema-instance
        /// </summary>
        public static readonly XNamespace XSI = @"http://www.w3.org/2001/XMLSchema-instance";

        /// <summary>
        /// http://www.w3.org/1999/xhtml
        /// </summary>
        public static readonly XNamespace XHTML = @"http://www.w3.org/1999/xhtml";


        /// <summary>
        /// MathML namespace
        /// http://www.w3.org/1998/Math/MathML
        /// </summary>
        public static readonly XNamespace MathML = @"http://www.w3.org/1998/Math/MathML";


        /// <summary>
        /// Prononsiation Lexicone namespace 
        /// http://www.w3.org/2005/01/pronunciation-lexicon
        /// </summary>
        public static readonly XNamespace PLex = @"http://www.w3.org/2005/01/pronunciation-lexicon";


        /// <summary>
        /// Synthesis namespace (Speech Synthesis Markup Language)
        ///  http://www.w3.org/2001/10/synthesis 
        /// </summary>
        public static readonly XNamespace Synthesis = @" http://www.w3.org/2001/10/synthesis ";


        /// <summary>
        /// SVG graphics namespace
        /// </summary>
        public static readonly XNamespace SVG = @"http://www.w3.org/2000/svg";
    }

    /// <summary>
    /// Namespaces used by Calibre
    /// </summary>
    public static class CalibreNamespaces
    {
        /// <summary>
        /// Calibre namespace    
        /// http://calibre.kovidgoyal.net/2009/metadata
        /// </summary>
        public static readonly XNamespace CalibreNamespace = @"http://calibre.kovidgoyal.net/2009/metadata";
    }

    /// <summary>
    /// Daisy (www.daisy.org) namespaces
    /// </summary>
    public static class DaisyNamespaces
    {
        /// <summary>
        /// NCX namespace 
        /// http://www.daisy.org/z3986/2005/ncx/
        /// </summary>
        public static readonly XNamespace NCXNamespace = @"http://www.daisy.org/z3986/2005/ncx/";
    }
}
