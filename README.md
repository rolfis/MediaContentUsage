Media Content Usage
===================

Event Handler and Property Editor for Media items in Umbraco 7.1.4, to find which Content references different Media.

Creates a new RelationType called "Relate Media and Content". Listens to Published events for Content and adds relations between Media and Content that can be found in different Properties for relevant DataTypes.

The relations can be read in Umbraco Backoffice in the Developer section, or with the provided Property Editor called "Media Content Usage". When a property of this type is placed on Media Items, it gives the editor information and links to referenced Content.

Media Content Usage also looks for relations in TinyMCE Property Editors using the "rel" attribute. Media Picker in TinyMCE for Umbraco 7.1.x writes the "rel" attribute with Media Nodeid. If you have old content from 6.x, you need to re-pick the image in the TinyMCE before Media Content Usage can find Media in TinyMCE properties like Body Text.

The DataTypes to search PropertyData for are configurable in web.config, with the key "MediaContentUsageDataTypeList". If this key is missing Media Content Usage searches PropertyData for the built-in and default shipped Richtext Editor and Media Pickers (-87,1035,1045). You can see your DataType id in the URL when editing the DataType in Umbraco Backoffice.

Example:
```
<appSettings>
    <add key="MediaContentUsageDataTypeList" value="-87,1035,1045,2100,2120" />
</appSettings>
```


Compile and Install
-------------------

If installing from source, compile into Chalmers.MediaContentUsage.dll and copy to bin folder. Also copy the App_Plugins folder to get the Property Editor.

Umbraco package
---------------

For Umbraco packages to install, [please see the project page on our](http://our.umbraco.org/projects/backoffice-extensions/media-content-usage).


Image property
--------------

Create a new DataType in the Developer section that uses "Chalmers.MediaContentUsage" (this is already done in the package). Add a property to your Image MediaType with this DataType. This will trigger the Property Editor and fetch relations to Content for the Image. 


Re-indexing
-----------

When installed for the first time, the new RelationType is created and all published Content is searched for Media relations. If you add DataTypes via web.config or want to re-index the relations this can be done by deleting the RelationType in Umbraco Backoffice and restarting the Application Pool. The RelationType will then be re-created and all relations re-indexed.


![alt text](https://raw.githubusercontent.com/rolfis/MediaContentUsage/master/MediaContentUsageExample.png)
