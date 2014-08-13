MediaContentUsage
=================

Property Editor for Media items in Umbraco 7.1.4, to find out which Content nodes references the media.

This hack relies on SQL and some very improveable queries. It looks trough the PropertyData for strings that match the Media NodeId. Combining dataInt, dataNvarchar and dataNtext into a string where column values are separated with a comma (,) it searches for ',(NodeId),' and 'rel="(NodeId)"' (Media Picker in TinyMCE writes this attribute now). 

The Data Types to search for in PropertyData are configurable with a reasonable default to the built-in TinyMCE and Media Picker.

It only searches on published Content nodes.


Improvements

To be really quick and more sustainable code, the relations between Content and Media should be written somewhere. The Relations API is maybee a good place. Write some code that hooks into different events like Publish and Delete, then update the Relations of say type "contentMediaRelation" and we have a much quicker way of knowing which Content uses the Media.
