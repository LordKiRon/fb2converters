﻿using ConverterContracts.ConversionElementsStyles;
using EPubLibrary.PathUtils;
using EPubLibrary.XHTML_Items;
using EPubLibraryContracts;
using EPubLibraryContracts.Settings;
using FB2EPubConverter.ElementConvertersV3.Epigraph;
using FB2EPubConverter.PrepearedHTMLFiles;
using FB2Library;
using FB2Library.Elements;
using XHTMLClassLibrary.BaseElements;
using XHTMLClassLibrary.BaseElements.BlockElements;

namespace FB2EPubConverter.ElementConvertersV3
{
    internal class Fb2EPubTextConverterV3 : BaseElementConverterV3
    {
        private readonly IEPubCommonSettings _commonSettings;
        private readonly ImageManager _images;
        private readonly HRefManagerV3 _referencesManager;
        private readonly IEPubV3Settings _v3Settings;

        private int _sectionCounter;

        public Fb2EPubTextConverterV3(IEPubCommonSettings commonSettings, ImageManager images, HRefManagerV3 referencesManager,IEPubV3Settings v3Settings)
        {
            _commonSettings = commonSettings;
            _images = images;
            _referencesManager = referencesManager;
            _v3Settings = v3Settings;
        }

        public void Convert(BookStructureManager bookStructureManager, FB2File fb2File)
        {
            // create second title page
            if ((fb2File.MainBody.Title != null) && (!string.IsNullOrEmpty(fb2File.MainBody.Title.ToString())))
            {
                string docTitle = fb2File.MainBody.Title.ToString();
                Logger.Log.DebugFormat("Adding section : {0}", docTitle);
                var addTitlePage = new BaseXHTMLFileV3
                {
                    PageTitle = docTitle,
                    FileEPubInternalPath = EPubInternalPath.GetDefaultLocation(DefaultLocations.DefaultTextFolder),
                    GuideRole = GuideTypeEnum.TitlePage,
                    Content = new Div(HTMLElementType.HTML5),
                    NavigationParent = null,
                    FileName = string.Format("section{0}.xhtml", ++_sectionCounter),
                    NotPartOfNavigation = true
                };
                var converterSettings = new ConverterOptionsV3
                {
                    CapitalDrop = _commonSettings.CapitalDrop,
                    Images = _images,
                    MaxSize = _v3Settings.HTMLFileMaxSize,
                    ReferencesManager = _referencesManager,
                };
                var titleConverter = new TitleConverterV3();
                addTitlePage.Content.Add(titleConverter.Convert(fb2File.MainBody.Title,
                    new TitleConverterParamsV3 { Settings = converterSettings, TitleLevel = 2 }));

                bookStructureManager.AddTitlePage(addTitlePage);
            }

            BaseXHTMLFileV3 mainDocument = null;
            if (!string.IsNullOrEmpty(fb2File.MainBody.Name))
            {
                string docTitle = fb2File.MainBody.Name;
                Logger.Log.DebugFormat("Adding section : {0}", docTitle);
                mainDocument = new BaseXHTMLFileV3
                {
                    PageTitle = docTitle,
                    FileEPubInternalPath = EPubInternalPath.GetDefaultLocation(DefaultLocations.DefaultTextFolder),
                    GuideRole = GuideTypeEnum.Text,
                    Content = new Div(HTMLElementType.HTML5),
                    NavigationParent = null,
                    FileName = string.Format("section{0}.xhtml", ++_sectionCounter)
                };
                bookStructureManager.AddBookPage(mainDocument);
            }

            if ((fb2File.MainBody.ImageName != null) && !string.IsNullOrEmpty(fb2File.MainBody.ImageName.HRef))
            {
                if (mainDocument == null)
                {
                    string newDocTitle = ((fb2File.MainBody.Title != null) && (!string.IsNullOrEmpty(fb2File.MainBody.Title.ToString()))) ? fb2File.MainBody.Title.ToString() : "main";
                    mainDocument = new BaseXHTMLFileV3
                    {
                        PageTitle = newDocTitle,
                        FileEPubInternalPath = EPubInternalPath.GetDefaultLocation(DefaultLocations.DefaultTextFolder),
                        GuideRole = GuideTypeEnum.Text,
                        Content = new Div(HTMLElementType.HTML5),
                        NavigationParent = null,
                        FileName = string.Format("section{0}.xhtml", ++_sectionCounter)
                    };
                    bookStructureManager.AddBookPage(mainDocument);
                }
                if (_images.IsImageIdReal(fb2File.MainBody.ImageName.HRef))
                {
                    var enclosing = new Div(HTMLElementType.HTML5); // we use the enclosing so the user can style center it
                    var converterSettings = new ConverterOptionsV3
                    {
                        CapitalDrop = _commonSettings.CapitalDrop,
                        Images = _images,
                        MaxSize = _v3Settings.HTMLFileMaxSize,
                        ReferencesManager = _referencesManager,
                    };

                    var imageConverter = new ImageConverterV3();
                    enclosing.Add(imageConverter.Convert(fb2File.MainBody.ImageName, new ImageConverterParamsV3 { Settings = converterSettings }));
                    SetClassType(enclosing, ElementStylesV3.BodyImage);
                    mainDocument.Content.Add(enclosing);
                }


            }
            foreach (var ep in fb2File.MainBody.Epigraphs)
            {
                if (mainDocument == null)
                {
                    string newDocTitle = ((fb2File.MainBody.Title != null) && (!string.IsNullOrEmpty(fb2File.MainBody.Title.ToString()))) ? fb2File.MainBody.Title.ToString() : "main";
                    mainDocument = new BaseXHTMLFileV3
                    {
                        PageTitle = newDocTitle,
                        FileEPubInternalPath = EPubInternalPath.GetDefaultLocation(DefaultLocations.DefaultTextFolder),
                        GuideRole = GuideTypeEnum.Text,
                        Content = new Div(HTMLElementType.HTML5),
                        NavigationParent = null,
                        FileName = string.Format("section{0}.xhtml", ++_sectionCounter)
                    };
                    bookStructureManager.AddBookPage(mainDocument);
                }
                var converterSettings = new ConverterOptionsV3
                {
                    CapitalDrop = _commonSettings.CapitalDrop,
                    Images = _images,
                    MaxSize = _v3Settings.HTMLFileMaxSize,
                    ReferencesManager = _referencesManager,
                };

                var epigraphConverter = new MainEpigraphConverterV3();
                mainDocument.Content.Add(epigraphConverter.Convert(ep,
                    new EpigraphConverterParamsV3 { Settings = converterSettings, Level = 1 }));
            }

            Logger.Log.Debug("Adding main sections");
            foreach (var section in fb2File.MainBody.Sections)
            {
                AddSection(bookStructureManager, section, mainDocument, false);
            }

            Logger.Log.Debug("Adding secondary bodies");
            foreach (var bodyItem in fb2File.Bodies)
            {
                if (bodyItem == fb2File.MainBody)
                {
                    continue;
                }
                bool fbeNotesSection = FBENotesSection(bodyItem.Name);
                if (fbeNotesSection)
                {
                    AddFbeNotesBody(bookStructureManager, bodyItem);
                }
                else
                {
                    AddSecondaryBody(bookStructureManager, bodyItem);
                }
            }
        }

        private void AddSection(BookStructureManager bookStructureManager, SectionItem section, BaseXHTMLFileV3 navParent, bool fbeNotesSection)
        {
            string docTitle = string.Empty;
            if (section.Title != null)
            {
                docTitle = section.Title.ToString();
            }
            Logger.Log.DebugFormat("Adding section : {0}", docTitle);
            BaseXHTMLFileV3 sectionDocument = null;
            bool firstDocumentOfSplit = true;
            var converterSettings = new ConverterOptionsV3
            {
                CapitalDrop = !fbeNotesSection && _commonSettings.CapitalDrop,
                Images = _images,
                MaxSize = _v3Settings.HTMLFileMaxSize,
                ReferencesManager = _referencesManager,
            };
            var sectionConverter = new SectionConverterV3
            {
                LinkSection = fbeNotesSection,
                RecursionLevel = GetRecursionLevel(navParent),
                Settings = converterSettings
            };
            foreach (var subitem in sectionConverter.Convert(section))
            {
                sectionDocument = fbeNotesSection ? new FB2NotesPageSectionFile() : new BaseXHTMLFileV3();
                sectionDocument.PageTitle = docTitle;
                sectionDocument.FileEPubInternalPath = EPubInternalPath.GetDefaultLocation(DefaultLocations.DefaultTextFolder);
                sectionDocument.GuideRole = (navParent == null) ? GuideTypeEnum.Text : navParent.GuideRole;
                sectionDocument.Content = subitem;
                sectionDocument.NavigationParent = navParent;
                sectionDocument.FileName = string.Format("section{0}.xhtml", ++_sectionCounter);

                if (!firstDocumentOfSplit || 
                    ((navParent!= null) && navParent.NotPartOfNavigation))
                {
                    sectionDocument.NotPartOfNavigation = true;
                }
                firstDocumentOfSplit = false;
                if (fbeNotesSection)
                {
                    bookStructureManager.AddFootnote(sectionDocument);
                }
                else
                {
                    bookStructureManager.AddBookPage(sectionDocument);
                }

            }
            Logger.Log.Debug("Adding sub-sections");
            foreach (var subSection in section.SubSections)
            {
                AddSection(bookStructureManager, subSection, sectionDocument, fbeNotesSection);
            }
        }

        private static int GetRecursionLevel(BaseXHTMLFileV3 navParent)
        {
            if (navParent == null)
            {
                return 1;
            }
            return navParent.NavigationLevel + 1;
        }

        /// <summary>
        /// Add and convert FBE style generated notes sections
        /// </summary>
        /// <param name="bookStructureManager"></param>
        /// <param name="bodyItem"></param>
        private void AddFbeNotesBody(BookStructureManager bookStructureManager, BodyItem bodyItem)
        {
            switch (_v3Settings.FootnotesCreationMode)
            {
                case FootnotesGenerationMode.V2StyleSections:
                    AddV2StyleFbeNotesBody(bookStructureManager, bodyItem);
                    break;
                case FootnotesGenerationMode.Combined:
                    AddV2StyleFbeNotesBody(bookStructureManager, bodyItem);               
                    break;
                case FootnotesGenerationMode.V3Footnotes:
                    AddFootnotesData(bookStructureManager, bodyItem);
                    break;
            }
        }

        private void AddFootnotesData(BookStructureManager bookStructureManager, BodyItem bodyItem)
        {
            
        }

        private void AddV2StyleFbeNotesBody(BookStructureManager bookStructureManager, BodyItem bodyItem)
        {
            string docTitle = bodyItem.Name;
            Logger.Log.DebugFormat("Adding section : {0}", docTitle);
            var sectionDocument = new FB2NotesPageSectionFile
            {
                PageTitle = docTitle,
                FileEPubInternalPath = EPubInternalPath.GetDefaultLocation(DefaultLocations.DefaultTextFolder),
                GuideRole = GuideTypeEnum.Glossary,
                Content = new Div(HTMLElementType.HTML5),
                NavigationParent = null,
                NotPartOfNavigation = true,
                FileName = string.Format("section{0}.xhtml", ++_sectionCounter)
            };
            if (bodyItem.Title != null)
            {
                var converterSettings = new ConverterOptionsV3
                {
                    CapitalDrop = false,
                    Images = _images,
                    MaxSize = _v3Settings.HTMLFileMaxSize,
                    ReferencesManager = _referencesManager,
                };
                var titleConverter = new TitleConverterV3();
                sectionDocument.Content.Add(titleConverter.Convert(bodyItem.Title,
                    new TitleConverterParamsV3 { Settings = converterSettings, TitleLevel = 1 }));
            }
            bookStructureManager.AddFootnote(sectionDocument);

            Logger.Log.Debug("Adding sub-sections");
            foreach (var section in bodyItem.Sections)
            {
                AddSection(bookStructureManager, section, sectionDocument, true);
            }
        }

        /// <summary>
        /// Add and convert generic secondary body section
        /// </summary>
        /// <param name="bookStructureManager"></param>
        /// <param name="bodyItem"></param>
        private void AddSecondaryBody(BookStructureManager bookStructureManager, BodyItem bodyItem)
        {
            string docTitle = string.Empty;
            if (string.IsNullOrEmpty(bodyItem.Name))
            {
                if (bodyItem.Title != null)
                {
                    docTitle = bodyItem.Title.ToString();
                }
            }
            else
            {
                docTitle = bodyItem.Name;
            }
            Logger.Log.DebugFormat("Adding section : {0}", docTitle);
            var sectionDocument = new BaseXHTMLFileV3
            {
                FileEPubInternalPath = EPubInternalPath.GetDefaultLocation(DefaultLocations.DefaultTextFolder),
                GuideRole = GuideTypeEnum.Text,
                Content = new Div(HTMLElementType.HTML5),
                NavigationParent = null,
                NotPartOfNavigation = false,
                FileName = string.Format("section{0}.xhtml", ++_sectionCounter),
                PageTitle = docTitle,
            };
            if (bodyItem.Title != null)
            {
                var converterSettings = new ConverterOptionsV3
                {
                    CapitalDrop = _commonSettings.CapitalDrop,
                    Images = _images,
                    MaxSize = _v3Settings.HTMLFileMaxSize,
                    ReferencesManager = _referencesManager,
                };
                var titleConverter = new TitleConverterV3();
                sectionDocument.Content.Add(titleConverter.Convert(bodyItem.Title,
                    new TitleConverterParamsV3 { Settings = converterSettings, TitleLevel = 1 }));
            }
            bookStructureManager.AddBookPage(sectionDocument);

            Logger.Log.Debug("Adding sub-sections");
            foreach (var section in bodyItem.Sections)
            {
                AddSection(bookStructureManager, section, sectionDocument, false);
            }
        }

        /// <summary>
        /// Check if the body is FBE generated notes section
        /// </summary>
        /// <param name="name">body name</param>
        /// <returns>true if it is FBE generated notes/comments body</returns>
        private static bool FBENotesSection(string name)
        {
            switch (name)
            {
                case "comments":
                case "footnotes":
                case "notes":
                    return true;
            }
            return false;
        }

    }
}
