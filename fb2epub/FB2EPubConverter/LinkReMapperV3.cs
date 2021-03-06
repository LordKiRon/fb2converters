﻿using System.Collections.Generic;
using System.Linq;
using ConverterContracts.ConversionElementsStyles;
using EPubLibrary.ReferenceUtils;
using EPubLibrary.V3ePubType;
using EPubLibrary.XHTML_Items;
using EPubLibraryContracts;
using EPubLibraryContracts.Settings;
using FB2EPubConverter.PrepearedHTMLFiles;
using XHTMLClassLibrary.BaseElements;
using XHTMLClassLibrary.BaseElements.BlockElements;
using XHTMLClassLibrary.BaseElements.InlineElements;
using XHTMLClassLibrary.BaseElements.InlineElements.TextBasedElements;

namespace FB2EPubConverter
{
    internal class LinkReMapperV3
    {
        private readonly string _idString;
        private readonly HTMLItem _linkTargetItem;
        private readonly BaseXHTMLFileV3 _linkTargetDocument;
        private readonly IHTMLItem _linkParentContainer;
        private readonly KeyValuePair<string, List<Anchor>> _link;
        private readonly BookStructureManager _structureManager;
        private readonly IDictionary<string, HTMLItem> _ids;
        private readonly IEPubV3Settings _v3Settings;
        
        private int _linksCount;

        public LinkReMapperV3(KeyValuePair<string, List<Anchor>> link, IDictionary<string, HTMLItem> ids, BookStructureManager structureManager, IEPubV3Settings v3Settings)
        {
            _link = link;
            _ids = ids;
            _v3Settings = v3Settings;
            _structureManager = structureManager;
            _idString = ReferencesUtils.GetIdFromLink(link.Key); // Get ID of a link target;
            _linkTargetItem = ids[_idString]; // get object targeted by link
            _linkTargetDocument = GetItemParentDocument(_linkTargetItem); // get parent document (file) containing targeted object
            if (_linkTargetDocument != null) // if link target container document (document containing item with ID we jump to) found 
            {
                _linkParentContainer = DetectItemParentContainer(_linkTargetItem); // get parent container of link target item
            }
        }

        public void Remap()
        {
            if (_linkTargetDocument == null)
            {
                Logger.Log.Error(string.Format("Internal consistency error - Used ID ({0}) has to be in one of the book documents objects", _linkTargetItem));
                return;
            }
            if (_linkParentContainer == null) // if no parent container found , means the link is directly to document , which can't be , so we ignore
            {
                Logger.Log.Error(string.Format("Internal consistency error - target link item ( {0} )has no parent container", _linkTargetItem));
                return;
            }
            if (_linkTargetDocument is FB2NotesPageSectionFile) // if it's FBE notes section
            {
                RemapLinkSecionReference();
            }
            else
            {
                RemapNormalReference();
            }
        }

        private void RemapNormalReference() 
        {
            foreach (var anchor in _link.Value) // iterate over all anchors (references) pointing to this link target (destination)
            {
                BaseXHTMLFileV3 anchorDocument = GetItemParentDocument(anchor); // get document containing anchor (link) pointing to target ID
                if (anchorDocument == null) // if anchor not contained (not found) in any document
                {
                    Logger.Log.Error(string.Format("Internal consistency error - anchor ({0}) for id ({1}) not contained (not found) in any document", anchor, _linkTargetItem));
                    continue;
                }
                // if in same document - local reference (link) , if in different - create link to that document
                anchor.HRef.Value = (anchorDocument == _linkTargetDocument) ? GenerateLocalLinkReference(_idString) : GenerateFarLinkReference(_idString, _linkTargetDocument.FileName);
            }                   
        }

        private void RemapLinkSecionReference()
        {
            switch (_v3Settings.FootnotesCreationMode)
            {
                case FootnotesGenerationMode.Combined:
                    RemepLinkSectionV2Style();
                    if (_v3Settings.FootnotesCreationMode == FootnotesGenerationMode.Combined)
                    {
                        EPubV3VocabularyStyles linkStyles = new EPubV3VocabularyStyles();
                        linkStyles.SetType(EpubV3Vocabulary.NoteRef);
                        _linkTargetItem.CustomAttributes.Add(linkStyles.GetAsCustomAttribute());
                    }

                    break;
                case FootnotesGenerationMode.V2StyleSections:
                    RemepLinkSectionV2Style();
                    break;
                case FootnotesGenerationMode.V3Footnotes:
                    GenerateFootnotes();
                    break;
            }
        }

        private void GenerateFootnotes()
        {
            foreach (var anchor in _link.Value) // iterate over all anchors (references) pointing to this link target (destination)
            {
                // generate a footnote section for this anchor in it's document
                // this might create several copies of the same footnote in different documents that linked to target
                GenerateSingleFootnote(anchor, _idString);
            }
        }

        private void GenerateSingleFootnote(Anchor anchor,string targetID)
        {
            BaseXHTMLFileV3 anchorDocument = GetItemParentDocument(anchor); // get document containing anchor pointing to target ID
            if (anchorDocument == null) // if anchor not contained (not found) in any document
            {
                Logger.Log.Error(string.Format("Internal consistency error - anchor ({0}) for id ({1}) not contained (not found) in any document", anchor, _linkTargetItem));
                return;
            }
            if (anchorDocument is FB2NotesPageSectionFile) // if anchor in FB2 Notes section (meaning link from FB2 notes to FB2 notes) no need to do anything as we do not save notes files in this mode
            {
                return;
            }

            // update reference link for an anchor, local one (without file name)
            anchor.HRef.Value = GenerateLocalLinkReference(targetID);
            // mark anchor (link reference) as note reference to make it pop-up able 
            EPubV3VocabularyStyles linkStyles = new EPubV3VocabularyStyles();
            linkStyles.SetType(EpubV3Vocabulary.NoteRef);
            anchor.CustomAttributes.Add(linkStyles.GetAsCustomAttribute());

            // add footnote object to the same document that contains link ,so the pop up point and pop up content will be in a same document
            var footnoteToAdd = CreateFootnoteItemFromTargetNoteItem(targetID);
            anchorDocument.AddFootNote(targetID, footnoteToAdd); // if footnote target ID already exist it will not add it
        }

        private HTMLItem CreateFootnoteItemFromTargetNoteItem(string targetId)
        {
            // Create copy (clone) of a referenced item
            var resultItem =  (HTMLItem)_ids[targetId].Clone();

            // get list of all links inside the pop up (footnote) item, that end up popinting to the items in FB2 Notes section
            var listOfContainedAnchors = GetAllFirstLevelContainedAnchors(resultItem).Where(x => GetItemParentDocument(x) is FB2NotesPageSectionFile);
            foreach (var anchor in listOfContainedAnchors.ToList())
            {
                GenerateSingleFootnote(anchor,targetId);
            }
            return resultItem;
        }


        private IEnumerable<Anchor> GetAllFirstLevelContainedAnchors(IHTMLItem linkItem)
        {
            if (linkItem.SubElements() == null) // if no sub elements - simple text item can't be anchor, so return empty list
            {
                return new List<Anchor>();
            }
            IEnumerable<Anchor> containedAnchors = linkItem.SubElements().OfType<Anchor>();
            return containedAnchors;
        }

        private IEnumerable<Anchor> GetAllContainedAnchors(IHTMLItem linkItem)
        {
            if (linkItem.SubElements() == null) // if no sub elements - simple text item can't be anchor, so return empty list
            {
                return new List<Anchor>();
            }
            IEnumerable<Anchor> containedAnchors =  linkItem.SubElements().OfType<Anchor>();
            return linkItem.SubElements().Aggregate(containedAnchors, (current, subElement) => current.Concat(GetAllContainedAnchors(subElement)));
        }

        private void RemepLinkSectionV2Style()
        {
            foreach (var anchor in _link.Value)
            {
                BaseXHTMLFileV3 anchorDocument = GetItemParentDocument(anchor); // get document containing anchor pointing to target ID
                if (anchorDocument == null) // if anchor not contained (not found) in any document
                {
                    Logger.Log.Error(string.Format("Internal consistency error - anchor ({0}) for id ({1}) not contained (not found) in any document", anchor, _linkTargetItem));
                    continue;
                }
                string backlinkRef;
                if (anchorDocument == _linkTargetDocument) // if anchor (link) and target (id) located in same document
                {
                    anchor.HRef.Value = GenerateLocalLinkReference(_idString);// update reference link for an anchor, local one (without file name)
                    backlinkRef = GenerateLocalLinkReference(anchor.GlobalAttributes.ID.Value as string); // in case we going to insert backlin - create a local reference
                }
                else // if they are located in different documents
                {
                    anchor.HRef.Value = GenerateFarLinkReference(_idString, _linkTargetDocument.FileName); // update reference link for an anchor, "far" one (with, pointing to another file name)
                    backlinkRef = GenerateFarLinkReference(anchor.GlobalAttributes.ID.Value as string, anchorDocument.FileName); // in case we going to insert backlin - create a "far" reference
                }
                var backLinkAnchor = new Anchor(_linkParentContainer.HTMLStandard);
                backLinkAnchor.HRef.Value = backlinkRef;
                backLinkAnchor.GlobalAttributes.Class.Value = ElementStylesV3.NoteAnchor;
                _linkParentContainer.Add(new EmptyLine(_linkParentContainer.HTMLStandard));
                _linkParentContainer.Add(backLinkAnchor);
                _linksCount++;
                backLinkAnchor.Add(new SimpleHTML5Text(backLinkAnchor.HTMLStandard) { Text = (_link.Value.Count > 1) ? string.Format("(<< back {0})  ", _linksCount) : string.Format("(<< back)  ") });
            }
        }

        private string GenerateLocalLinkReference(string idToReference)
        {
            return string.Format("#{0}", idToReference);
        }

        private string GenerateFarLinkReference(string idToReference, string fileName)
        {
            return string.Format("{0}#{1}", fileName, idToReference);
        }



        /// <summary>
        /// Returns parent (file level) document (document containing) for the item
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private BaseXHTMLFileV3 GetItemParentDocument(IHTMLItem value)
        {
            return _structureManager.GetItemParentDocument(value) as BaseXHTMLFileV3;
        }

        /// <summary>
        /// Detect parent container of the element
        /// </summary>
        /// <param name="referencedItem"></param>
        /// <returns></returns>
        private IHTMLItem DetectItemParentContainer(IHTMLItem referencedItem)
        {
            if (referencedItem is IBlockElement) // if item itself is container - return it
            {
                return referencedItem;
            }
            if (referencedItem.Parent != null)
            {
                if (referencedItem.Parent is IBlockElement) // if item is located inside container
                {
                    return referencedItem.Parent;
                }
                if (referencedItem.Parent is TextBasedElement) // if parent is text, i's ok for container
                {
                    return referencedItem.Parent;
                }
                return DetectItemParentContainer(referencedItem.Parent); // go up the inclusion chain
            }

            return null;
        }


    }
}
