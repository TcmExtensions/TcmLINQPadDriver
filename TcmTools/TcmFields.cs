using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Tridion.ContentManager.CoreService.Client;
using System.Collections;

public static class TcmFieldsExtensionMethods
{
    public static TcmFields ContentFields(this ComponentData component, SchemaFieldsData schema)
    {
        return TcmFields.ForContentOf(schema, component);
    }
    public static TcmFields MetadataFields(this ComponentData component, SchemaFieldsData schema)
    {
        return TcmFields.ForMetadataOf(schema, component);
    }
}

/// <summary>
/// A wrapper around the content or metadata fields of a Tridion item.
/// </summary>
public class TcmFields
{
    private ItemFieldDefinitionData[] definitions;
    private XmlNamespaceManager namespaceManager;

    private XmlElement root; // the root element under which these fields live

    // at any point EITHER data OR parent has a value
    private SchemaFieldsData data; // the schema fields data as retrieved from the core service
    private TcmFields parent; // the parent fields (so we're an embedded schema), where we can find the data

    public TcmFields(SchemaFieldsData _data, ItemFieldDefinitionData[] _definitions, string _content = null, string _rootElementName = null)
    {
        data = _data;
        definitions = _definitions;
        var content = new XmlDocument();
        if (!string.IsNullOrEmpty(_content))
        {
            content.LoadXml(_content);
        }
        else
        {
            content.AppendChild(content.CreateElement(string.IsNullOrEmpty(_rootElementName) ? _data.RootElementName : _rootElementName, _data.NamespaceUri));
        }
        root = content.DocumentElement;
        namespaceManager = new XmlNamespaceManager(content.NameTable);
        namespaceManager.AddNamespace("custom", _data.NamespaceUri);
    }
    public TcmFields(TcmFields _parent, ItemFieldDefinitionData[] _definitions, XmlElement _root)
    {
        definitions = _definitions;
        parent = _parent;
        root = _root;
    }

    public static TcmFields ForContentOf(SchemaFieldsData _data)
    {
        return new TcmFields(_data, _data.Fields);
    }
    public static TcmFields ForContentOf(SchemaFieldsData _data, ComponentData _component)
    {
        return new TcmFields(_data, _data.Fields, _component.Content);
    }
    public static TcmFields ForMetadataOf(SchemaFieldsData _data, RepositoryLocalObjectData _item)
    {
        return new TcmFields(_data, _data.MetadataFields, _item.Metadata, "Metadata");
    }

    public string NamespaceUri
    {
        get { return data != null ? data.NamespaceUri : parent.NamespaceUri; }
    }
    public XmlNamespaceManager NamespaceManager
    {
        get { return parent != null ? parent.namespaceManager : namespaceManager; }
    }

    internal IEnumerable<XmlElement> GetFieldElements(ItemFieldDefinitionData definition)
    {
        return root.SelectNodes("custom:" + definition.Name, NamespaceManager).OfType<XmlElement>();
    }
    internal XmlElement AddFieldElement(ItemFieldDefinitionData definition)
    {
        var newElement = root.OwnerDocument.CreateElement(definition.Name, NamespaceUri);

        XmlNodeList nodes = root.SelectNodes("custom:" + definition.Name, NamespaceManager);
        XmlElement referenceElement = null;
        if (nodes.Count > 0)
        {
            referenceElement = (XmlElement)nodes[nodes.Count - 1];
        }
        else
        {
            // this is the first value for this field, find its position in the XML based on the field order in the schema
            bool foundUs = false;
            for (int i = definitions.Length - 1; i >= 0; i--)
            {
                if (!foundUs)
                {
                    if (definitions[i].Name == definition.Name)
                    {
                        foundUs = true;
                    }
                }
                else
                {
                    var values = GetFieldElements(definitions[i]);
                    if (values.Count() > 0)
                    {
                        referenceElement = values.Last();
                        break; // from for loop
                    }
                }
            } // for every definition in reverse order
        } // no existing values found
        root.InsertAfter(newElement, referenceElement); // if referenceElement is null, will insert as first child
        return newElement;
    }

    public IEnumerator<TcmField> GetEnumerator()
    {
        return new TcmFieldEnumerator(this, definitions);
    }
    public bool Exists(string _name)
    {
        return definitions.Any(def => def.Name == _name);
    }
    public TcmField this[string _name]
    {
        get
        {
            var definition = definitions.First(def => def.Name == _name);
            if (definition == null) throw new ArgumentOutOfRangeException("Unknown field '" + _name + "'");
            return new TcmField(this, definition);
        }
    }

    public override string ToString()
    {
        // TODO: remove elements for fields that don't have a value
        return root.OuterXml;
    }

}

public class TcmFieldEnumerator : IEnumerator<TcmField>
{
    private TcmFields _tcmFields;
    private ItemFieldDefinitionData[] definitions;

    // Enumerators are positioned before the first element until the first MoveNext() call
    int position = -1;

    public TcmFieldEnumerator(TcmFields tcmFields, ItemFieldDefinitionData[] _definitions)
    {
        _tcmFields = tcmFields;
        definitions = _definitions;
    }

    public bool MoveNext()
    {
        position++;
        return (position < definitions.Length);
    }

    public void Reset()
    {
        position = -1;
    }

    object IEnumerator.Current
    {
        get
        {
            return Current;
        }
    }

    public TcmField Current
    {
        get
        {
            try
            {
                return new TcmField(_tcmFields, definitions[position]);
            }
            catch (IndexOutOfRangeException)
            {
                throw new InvalidOperationException();
            }
        }
    }

    public void Dispose()
    {
    }
}

public class TcmField
{
    private TcmFields _tcmFields;
    private ItemFieldDefinitionData definition;

    public TcmField(TcmFields tcmFields, ItemFieldDefinitionData _definition)
    {
        _tcmFields = tcmFields;
        definition = _definition;
    }

    public string Name
    {
        get { return definition.Name; }
    }
    public Type Type
    {
        get { return definition.GetType(); }
    }
    public string Value
    {
        get
        {
            return Values.Count > 0 ? Values[0] : null;
        }
        set
        {
            if (Values.Count == 0) _tcmFields.AddFieldElement(definition);
            Values[0] = value;
        }
    }
    public TcmFieldValueCollection Values
    {
        get
        {
            return new TcmFieldValueCollection(_tcmFields, definition);
        }
    }

    public void AddValue(string value = null)
    {
        XmlElement newElement = _tcmFields.AddFieldElement(definition);
        if (value != null) newElement.InnerText = value;
    }

    public void RemoveValue(string value)
    {
        var elements = _tcmFields.GetFieldElements(definition);
        foreach (var element in elements)
        {
            if (element.InnerText == value)
            {
                element.ParentNode.RemoveChild(element);
            }
        }
    }

    public void RemoveValue(int i)
    {
        var elements = _tcmFields.GetFieldElements(definition).ToArray();
        elements[i].ParentNode.RemoveChild(elements[i]);
    }

    public IEnumerable<TcmFields> SubFields
    {
        get
        {
            var embeddedFieldDefinition = definition as EmbeddedSchemaFieldDefinitionData;
            if (embeddedFieldDefinition != null)
            {
                var elements = _tcmFields.GetFieldElements(definition);
                foreach (var element in elements)
                {
                    yield return new TcmFields(_tcmFields, embeddedFieldDefinition.EmbeddedFields, (XmlElement)element);
                }
            }
        }
    }

    public TcmFields GetSubFields(int i = 0)
    {
        var embeddedFieldDefinition = definition as EmbeddedSchemaFieldDefinitionData;
        if (embeddedFieldDefinition != null)
        {
            var elements = _tcmFields.GetFieldElements(definition);
            if (i == 0 && !elements.Any())
            {
                // you can always set the first value of any field without calling AddValue, so same applies to embedded fields
                AddValue();
                elements = _tcmFields.GetFieldElements(definition);
            }
            return new TcmFields(_tcmFields, embeddedFieldDefinition.EmbeddedFields, elements.ToArray()[i]);
        }
        else
        {
            throw new InvalidOperationException("You can only GetSubField on an EmbeddedSchemaField");
        }
    }
    // The subfield with the given name of this field
    public TcmField this[string name]
    {
        get { return GetSubFields()[name]; }
    }
    // The subfields of the given value of this field
    public TcmFields this[int i]
    {
        get { return GetSubFields(i); }
    }

}

public class TcmFieldValueCollection
{
    private TcmFields _tcmFields;
    private ItemFieldDefinitionData definition;

    public TcmFieldValueCollection(TcmFields tcmFields, ItemFieldDefinitionData _definition)
    {
        _tcmFields = tcmFields;
        definition = _definition;
    }

    public int Count
    {
        get { return _tcmFields.GetFieldElements(definition).Count(); }
    }

    public bool IsLinkField
    {
        get { return definition is ComponentLinkFieldDefinitionData || definition is ExternalLinkFieldDefinitionData || definition is MultimediaLinkFieldDefinitionData; }
    }

    public bool IsRichTextField
    {
        get { return definition is XhtmlFieldDefinitionData; }
    }

    public bool IsEmbeddedField
    {
        get { return definition is EmbeddedSchemaFieldDefinitionData;  }
    }

    public string this[int i]
    {
        get
        {
            XmlElement[] elements = _tcmFields.GetFieldElements(definition).ToArray();
            if (i >= elements.Length) throw new IndexOutOfRangeException();
            if (IsLinkField)
            {
                return elements[i].Attributes["xlink:href"].Value;
            }
            else
            {
                return elements[i].InnerXml;
            }
        }
        set
        {
            XmlElement[] elements = _tcmFields.GetFieldElements(definition).ToArray<XmlElement>();
            if (i >= elements.Length) throw new IndexOutOfRangeException();
            if (IsLinkField)
            {
                elements[i].SetAttribute("href", "http://www.w3.org/1999/xlink", value);
                elements[i].SetAttribute("type", "http://www.w3.org/1999/xlink", "simple");
                // TODO: should we clear the title for MMCLink and CLink fields? They will automatically be updated when we save the xlink:href.
            }
            else if (IsRichTextField)
            {
                elements[i].InnerXml = value;
            }
            else
            {
                elements[i].InnerText = value;
            }
        }
    }

    public IEnumerator<string> GetEnumerator()
    {
        return _tcmFields.GetFieldElements(definition).Select(elm => 
            IsLinkField ? elm.Attributes["xlink:href"].Value : elm.InnerXml.ToString()
        ).GetEnumerator();
    }
}
