//===============================================================================================================
// System  : Personal Data Interchange Classes
// File    : VCardParser.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 11/18/2014
// Note    : Copyright 2004-2014, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to parse vCard Personal Data Interchange (PDI) data streams.  It supports
// both the vCard 2.1 and vCard 3.0 specification file formats.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/PDI.
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 03/21/2004  EFW  Created the code
// 03/17/2007  EFW  Updated for use with .NET 2.0
//===============================================================================================================

using System;
using System.Globalization;
using System.IO;

using EWSoftware.PDI.Objects;
using EWSoftware.PDI.Properties;

namespace EWSoftware.PDI.Parser
{
    /// <summary>
    /// This class implements the parser that handles vCard PDI objects
    /// </summary>
    public class VCardParser : PDIParser
    {
        #region Private data members
        //=====================================================================

        private VCard currentCard;          // The current vCard being processed
        private VCardCollection vCards;     // The collection of vCards

        //=====================================================================

        // This private array is used to translate property names into property types
        private static NameToValue<PropertyType>[] ntv = {
            new NameToValue<PropertyType>("BEGIN", PropertyType.Begin),
            new NameToValue<PropertyType>("END", PropertyType.End),
            new NameToValue<PropertyType>("VERSION", PropertyType.Version),
            new NameToValue<PropertyType>("PROFILE", PropertyType.Profile),
            new NameToValue<PropertyType>("NAME", PropertyType.MimeName),
            new NameToValue<PropertyType>("SOURCE", PropertyType.MimeSource),
            new NameToValue<PropertyType>("PRODID", PropertyType.ProductId),
            new NameToValue<PropertyType>("NICKNAME", PropertyType.Nickname),
            new NameToValue<PropertyType>("SORT-STRING", PropertyType.SortString),
            new NameToValue<PropertyType>("CLASS", PropertyType.Class),
            new NameToValue<PropertyType>("CATEGORIES", PropertyType.Categories),
            new NameToValue<PropertyType>("FN", PropertyType.FormattedName),
            new NameToValue<PropertyType>("N", PropertyType.Name),
            new NameToValue<PropertyType>("TITLE", PropertyType.Title),
            new NameToValue<PropertyType>("ROLE", PropertyType.Role),
            new NameToValue<PropertyType>("MAILER", PropertyType.Mailer),
            new NameToValue<PropertyType>("URL", PropertyType.Url),
            new NameToValue<PropertyType>("ORG", PropertyType.Organization),
            new NameToValue<PropertyType>("UID", PropertyType.UniqueId),
            new NameToValue<PropertyType>("BDAY", PropertyType.BirthDate),
            new NameToValue<PropertyType>("REV", PropertyType.Revision),
            new NameToValue<PropertyType>("TZ", PropertyType.TimeZone),
            new NameToValue<PropertyType>("GEO", PropertyType.GeographicPosition),
            new NameToValue<PropertyType>("KEY", PropertyType.PublicKey),
            new NameToValue<PropertyType>("PHOTO", PropertyType.Photo),
            new NameToValue<PropertyType>("LOGO", PropertyType.Logo),
            new NameToValue<PropertyType>("SOUND", PropertyType.Sound),
            new NameToValue<PropertyType>("NOTE", PropertyType.Note),
            new NameToValue<PropertyType>("ADR", PropertyType.Address),
            new NameToValue<PropertyType>("LABEL", PropertyType.Label),
            new NameToValue<PropertyType>("TEL", PropertyType.Telephone),
            new NameToValue<PropertyType>("EMAIL", PropertyType.EMail),
            new NameToValue<PropertyType>("AGENT", PropertyType.Agent),

            // The last entry should always be CustomProperty to catch all unrecognized properties.  The actual
            // property name is not relevant.
            new NameToValue<PropertyType>("X-", PropertyType.Custom)
        };
        #endregion

        #region Properties
        //=====================================================================

        /// <summary>
        /// This is used to get the collection of vCards parsed from the PDI data stream
        /// </summary>
        /// <remarks>The vCards from prior calls to the parsing methods are not cleared automatically.  Call
        /// <c>VCards.Clear()</c> before calling a parsing method if you do not want to retain the vCards from
        /// prior runs.</remarks>
        public VCardCollection VCards
        {
            get { return vCards; }
        }
        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <overloads>There are two overloads for the constructor</overloads>
        public VCardParser()
        {
            vCards = new VCardCollection();
        }

        /// <summary>
        /// This version of the constructor is used when parsing vCard data that is to be stored in an existing
        /// vCard instance.
        /// </summary>
        /// <remarks>The properties in the passed vCard will be cleared</remarks>
        /// <param name="vCard">The existing vCard instance</param>
        /// <exception cref="ArgumentNullException">This is thrown if the specified vCard object is null</exception>
        protected VCardParser(VCard vCard) : this()
        {
            if(vCard == null)
                throw new ArgumentNullException("vCard", LR.GetString("ExParseNullObject", "vCard"));

            currentCard = vCard;
            currentCard.ClearProperties();
            vCards.Add(vCard);
        }
        #endregion

        #region Methods
        //=====================================================================

        /// <summary>
        /// This static method can be used to load property values into a new instance of a single vCard from a
        /// string.
        /// </summary>
        /// <param name="vCardText">A set of properties for a single vCard in a string</param>
        /// <returns>A new vCard instance as created from the string</returns>
        /// <example>
        /// <code language="cs">
        /// VCard vCard = VCardParser.ParseFromString(oneVCard);
        /// </code>
        /// <code language="vbnet">
        /// Dim vCard As VCard = VCardParser.ParseFromString(oneVCard)
        /// </code>
        /// </example>
        public static VCard ParseFromString(string vCardText)
        {
            VCardParser vcp = new VCardParser();
            vcp.ParseString(vCardText);

            return vcp.VCards[0];
        }

        /// <summary>
        /// This static method can be used to load property values into an existing instance of a single vCard
        /// from a string.
        /// </summary>
        /// <param name="vCardText">A set of properties for a single vCard in a string</param>
        /// <param name="vCard">The vCard instance into which the properties will be loaded</param>
        /// <remarks>The properties of the specified vCard will be cleared before the new properties are loaded
        /// into it.</remarks>
        /// <example>
        /// <code language="cs">
        /// VCard vCard = new VCard();
        /// VCardParser.ParseFromString(oneVCard, vCard);
        /// </code>
        /// <code language="vbnet">
        /// Dim vCard As New VCard
        /// VCardParser.ParseFromString(oneVCard, vCard)
        /// </code>
        /// </example>
        public static void ParseFromString(string vCardText, VCard vCard)
        {
            VCardParser vcp = new VCardParser(vCard);
            vcp.ParseString(vCardText);
        }

        /// <summary>
        /// This static method can be used to load property values into a new vCard collection from a string
        /// containing one or more vCards.
        /// </summary>
        /// <param name="vCards">A set of properties for one or more vCards in a string</param>
        /// <returns>A new vCard collection as created from the string</returns>
        /// <example>
        /// <code language="cs">
        /// VCardCollection vCards = VCardParser.ParseSetFromString(vCards);
        /// </code>
        /// <code language="vbnet">
        /// Dim vCards As VCardCollection = VCardParser.ParseSetFromString(vCards)
        /// </code>
        /// </example>
        public static VCardCollection ParseSetFromString(string vCards)
        {
            VCardParser vcp = new VCardParser();
            vcp.ParseString(vCards);

            return vcp.VCards;
        }

        /// <summary>
        /// This static method can be used to load property values into a new vCard collection from a file.  The
        /// filename can be a disk file or a URL.
        /// </summary>
        /// <param name="filename">A path or URL to a file containing one or more vCards</param>
        /// <returns>A new vCard collection as created from the file</returns>
        /// <example>
        /// <code language="cs">
        /// VCardCollection vCards1 = VCardParser.ParseFromFile(@"C:\AddressBook.vcf");
        /// VCardCollection vCards2 = VCardParser.ParseFromFile(
        ///     "http://www.mydomain.com/VCards/AddressBook.vcf");
        /// </code>
        /// <code language="vbnet">
        /// Dim vCards1 As VCardCollection = VCardParser.ParseFromFile("C:\AddressBook.vcf")
        /// Dim vCards2 As VCardCollection = VCardParser.ParseFromFile(
        ///     "http://www.mydomain.com/VCards/AddressBook.vcf")
        /// </code>
        /// </example>
        public static VCardCollection ParseFromFile(string filename)
        {
            VCardParser vcp = new VCardParser();
            vcp.ParseFile(filename);

            return vcp.VCards;
        }

        /// <summary>
        /// This static method can be used to load property values into a new vCard collection from a
        /// <see cref="TextReader"/> derived object such as a <see cref="StreamReader"/> or a
        /// <see cref="StringReader"/>.
        /// </summary>
        /// <param name="stream">An IO stream from which to read the vCards.  It is up to you to open the stream
        /// with the appropriate text encoding method if necessary.</param>
        /// <returns>A new vCard collection as created from the IO stream</returns>
        /// <example>
        /// <code language="cs">
        /// StreamReader sr = new StreamReader(@"C:\Test.vcf");
        /// VCardCollection vCards1 = VCardParser.ParseFromStream(sr);
        /// sr.Close();
        /// </code>
        /// <code language="vbnet">
        /// Dim sr As New StreamReader("C:\Test.vcf")
        /// Dim vCards1 As VCardCollection = VCardParser.ParseFromStream(sr)
        /// sr.Close()
        /// </code>
        /// </example>
        public static VCardCollection ParseFromStream(TextReader stream)
        {
            VCardParser vcp = new VCardParser();
            vcp.ParseReader(stream);

            return vcp.VCards;
        }

        /// <summary>
        /// This is implemented to handle properties as they are parsed from the data stream
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="parameters">A string collection containing the parameters and their values.  If empty,
        /// there are no parameters.</param>
        /// <param name="propertyValue">The value of the property.</param>
        /// <remarks><para>There may be a mixture of name/value pairs or values alone in the parameters string
        /// collection.  It is up to the derived class to process the parameter list based on the specification
        /// to which it conforms.  For entries that are parameter names, the entry immediately following it in
        /// the collection is its associated parameter value.  The property name, parameter names, and their
        /// values may be in upper, lower, or mixed case.</para>
        /// 
        /// <para>The value may be an encoded string.  The properties are responsible for any decoding that may
        /// need to occur (i.e. base 64 encoded image data).</para></remarks>
        /// <exception cref="PDIParserException">This is thrown if an error is encountered while parsing the data
        /// stream.  Refer to the and inner exceptions for information on the cause of the problem.</exception>
        protected override void PropertyParser(string propertyName, StringCollection parameters, string propertyValue)
        {
            SpecificationVersions version = SpecificationVersions.None;
            string temp, group = null;
            int idx;

            // Parse out the group name if there is one
            idx = propertyName.IndexOf('.');

            if(idx != -1)
            {
                group = propertyName.Substring(0, idx).Trim();
                propertyName = propertyName.Substring(idx + 1).Trim();
            }

            // The last entry is always CustomProperty so scan for length minus one
            for(idx = 0; idx < ntv.Length - 1; idx++)
                if(ntv[idx].IsMatch(propertyName))
                    break;

            // An opening BEGIN:VCARD property must have been seen
            if(currentCard == null && ntv[idx].EnumValue != PropertyType.Begin)
                throw new PDIParserException(this.LineNumber, LR.GetString("ExParseNoBeginProp", "BEGIN:VCARD",
                    propertyName));

            // Handle or create the property
            switch(ntv[idx].EnumValue)
            {
                case PropertyType.Begin:
                    // The value must be VCARD
                    if(String.Compare(propertyValue.Trim(), "VCARD", StringComparison.OrdinalIgnoreCase) != 0)
                        throw new PDIParserException(this.LineNumber, LR.GetString("ExParseUnrecognizedTagValue",
                            ntv[idx].Name, propertyValue));

                    // NOTE: If serializing into an existing instance, this may not be null.  If so, it is
                    // ignored.
                    if(currentCard == null)
                    {
                        currentCard = new VCard();
                        vCards.Add(currentCard);
                    }

                    currentCard.Group = group;
                    break;

                case PropertyType.End:
                    // The value must be VCARD
                    if(String.Compare(propertyValue.Trim(), "VCARD", StringComparison.OrdinalIgnoreCase) != 0)
                        throw new PDIParserException(this.LineNumber, LR.GetString("ExParseUnrecognizedTagValue",
                            ntv[idx].Name, propertyValue));

                    // The group must match too
                    if(currentCard.Group != group)
                        throw new PDIParserException(this.LineNumber, LR.GetString("ExParseUnexpectedGroupTag",
                            currentCard.Group, group));

                    // When done, we'll propagate the version number to all objects to make it consistent
                    currentCard.PropagateVersion();

                    // The vCard is added to the collection when created so we don't have to rely on an END:VCARD
                    // to add it.
                    currentCard = null;
                    break;

                case PropertyType.Profile:
                    // The value must be VCARD
                    if(String.Compare(propertyValue.Trim(), "VCARD", StringComparison.OrdinalIgnoreCase) != 0)
                        throw new PDIParserException(this.LineNumber, LR.GetString("ExParseUnrecognizedTagValue",
                            ntv[idx].Name, propertyValue));

                    currentCard.AddProfile = true;
                    break;

                case PropertyType.Version:
                    // Version must be 2.1 or 3.0
                    temp = propertyValue.Trim();

                    if(temp == "2.1")
                        version = SpecificationVersions.vCard21;
                    else
                        if(temp == "3.0")
                            version = SpecificationVersions.vCard30;
                        else
                            throw new PDIParserException(this.LineNumber, LR.GetString("ExParseUnrecognizedVersion",
                                "vCard", temp));

                    currentCard.Version = version;
                    break;

                case PropertyType.MimeName:
                    currentCard.MimeName.EncodedValue = propertyValue;
                    break;

                case PropertyType.MimeSource:
                    currentCard.MimeSource.DeserializeParameters(parameters);
                    currentCard.MimeSource.EncodedValue = propertyValue;
                    break;

                case PropertyType.ProductId:
                    currentCard.ProductId.EncodedValue = propertyValue;
                    break;

                case PropertyType.Nickname:
                    currentCard.Nickname.DeserializeParameters(parameters);
                    currentCard.Nickname.EncodedValue = propertyValue;
                    currentCard.Nickname.Group = group;
                    break;

                case PropertyType.SortString:
                    currentCard.SortString.DeserializeParameters(parameters);
                    currentCard.SortString.EncodedValue = propertyValue;
                    currentCard.SortString.Group = group;
                    break;

                case PropertyType.Class:
                    currentCard.Classification.EncodedValue = propertyValue;
                    currentCard.Classification.Group = group;
                    break;

                case PropertyType.Categories:
                    currentCard.Categories.DeserializeParameters(parameters);
                    currentCard.Categories.EncodedValue = propertyValue;
                    currentCard.Categories.Group = group;
                    break;

                case PropertyType.FormattedName:
                    currentCard.FormattedName.DeserializeParameters(parameters);
                    currentCard.FormattedName.EncodedValue = propertyValue;
                    currentCard.FormattedName.Group = group;
                    break;

                case PropertyType.Name:
                    currentCard.Name.DeserializeParameters(parameters);
                    currentCard.Name.EncodedValue = propertyValue;
                    currentCard.Name.Group = group;
                    break;

                case PropertyType.Title:
                    currentCard.Title.DeserializeParameters(parameters);
                    currentCard.Title.EncodedValue = propertyValue;
                    currentCard.Title.Group = group;
                    break;

                case PropertyType.Role:
                    currentCard.Role.DeserializeParameters(parameters);
                    currentCard.Role.EncodedValue = propertyValue;
                    currentCard.Role.Group = group;
                    break;

                case PropertyType.Mailer:
                    currentCard.Mailer.DeserializeParameters(parameters);
                    currentCard.Mailer.EncodedValue = propertyValue;
                    currentCard.Mailer.Group = group;
                    break;

                case PropertyType.Url:
                    currentCard.Url.DeserializeParameters(parameters);
                    currentCard.Url.EncodedValue = propertyValue;
                    currentCard.Url.Group = group;
                    break;

                case PropertyType.Organization:
                    currentCard.Organization.DeserializeParameters(parameters);
                    currentCard.Organization.EncodedValue = propertyValue;
                    currentCard.Organization.Group = group;
                    break;

                case PropertyType.UniqueId:
                    currentCard.UniqueId.EncodedValue = propertyValue;
                    currentCard.UniqueId.Group = group;
                    break;

                case PropertyType.BirthDate:
                    currentCard.BirthDate.DeserializeParameters(parameters);
                    currentCard.BirthDate.EncodedValue = propertyValue;
                    currentCard.BirthDate.Group = group;
                    break;

                case PropertyType.Revision:
                    currentCard.LastRevision.DeserializeParameters(parameters);
                    currentCard.LastRevision.EncodedValue = propertyValue;
                    currentCard.LastRevision.Group = group;
                    break;

                case PropertyType.TimeZone:
                    currentCard.TimeZone.DeserializeParameters(parameters);
                    currentCard.TimeZone.EncodedValue = propertyValue;
                    currentCard.TimeZone.Group = group;
                    break;

                case PropertyType.GeographicPosition:
                    currentCard.GeographicPosition.EncodedValue = propertyValue;
                    currentCard.GeographicPosition.Group = group;
                    break;

                case PropertyType.PublicKey:
                    currentCard.PublicKey.DeserializeParameters(parameters);
                    currentCard.PublicKey.EncodedValue = propertyValue;
                    currentCard.PublicKey.Group = group;
                    break;

                case PropertyType.Photo:
                    currentCard.Photo.DeserializeParameters(parameters);
                    currentCard.Photo.EncodedValue = propertyValue;
                    currentCard.Photo.Group = group;
                    break;

                case PropertyType.Logo:
                    currentCard.Logo.DeserializeParameters(parameters);
                    currentCard.Logo.EncodedValue = propertyValue;
                    currentCard.Logo.Group = group;
                    break;

                case PropertyType.Sound:
                    currentCard.Sound.DeserializeParameters(parameters);
                    currentCard.Sound.EncodedValue = propertyValue;
                    currentCard.Sound.Group = group;
                    break;

                case PropertyType.Note:
                    NoteProperty n = new NoteProperty();
                    n.DeserializeParameters(parameters);
                    n.EncodedValue = propertyValue;
                    n.Group = group;
                    currentCard.Notes.Add(n);
                    break;

                case PropertyType.Address:
                    AddressProperty a = new AddressProperty();
                    a.DeserializeParameters(parameters);
                    a.EncodedValue = propertyValue;
                    a.Group = group;
                    currentCard.Addresses.Add(a);
                    break;

                case PropertyType.Label:
                    LabelProperty l = new LabelProperty();
                    l.DeserializeParameters(parameters);
                    l.EncodedValue = propertyValue;
                    l.Group = group;
                    currentCard.Labels.Add(l);
                    break;

                case PropertyType.Telephone:
                    TelephoneProperty t = new TelephoneProperty();
                    t.DeserializeParameters(parameters);
                    t.EncodedValue = propertyValue;
                    t.Group = group;
                    currentCard.Telephones.Add(t);
                    break;

                case PropertyType.EMail:
                    EMailProperty e = new EMailProperty();
                    e.DeserializeParameters(parameters);
                    e.EncodedValue = propertyValue;
                    e.Group = group;
                    currentCard.EMailAddresses.Add(e);
                    break;

                case PropertyType.Agent:
                    AgentProperty ag = new AgentProperty();
                    ag.DeserializeParameters(parameters);
                    ag.EncodedValue = propertyValue;
                    ag.Group = group;
                    currentCard.Agents.Add(ag);
                    break;

                default:    // Anything else is a custom property
                    CustomProperty c = new CustomProperty(propertyName);
                    c.DeserializeParameters(parameters);
                    c.EncodedValue = propertyValue;
                    c.Group = group;
                    currentCard.CustomProperties.Add(c);
                    break;
            }
        }
        #endregion
    }
}
