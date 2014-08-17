Media Content Usage
===================

Event Handler and Property Editor for Media items in Umbraco 7.1.4, to find which Content references different Media.

Creates a new RelationType called "Relate Media and Content". Listens to Published and UnPublished events for Content and adds or updates relations between Media and Content that can be found in different Properties for relevant DataTypes.

The relations can be read in Umbraco Backoffice in the Developer section, or with the provided Property Editor called "Media Content Usage". When a property of this type is placed on Media Items, it gives the editor information and links to referenced Content.

Media Content Usage also looks for relations in TinyMCE Property Editors using the "rel" attribute. Media Picker in TinyMCE for Umbraco 7.1.x writes the "rel" attribute with Media Nodeid. If you have old content from 6.x, you need to re-pick the image in the TinyMCE before Media Content Usage can find Media in TinyMCE properties like Body Text.

The DataTypes to search PropertyData for are configurable in web.config, with the key "MediaContentUsageDataTypeList". If this key is missing Media Content Usage searches PropertyData for the built-in Rich Text Editor and Media Pickers (-81,1035,1045).


Install
-------

If installing from source, compile into Chalmers.MediaContentUsage.dll and copy to bin folder. Also copy the App_Plugins folder to get the Property Editor. There is also packages in "Releases" that can be installed in the Developer section of Umbraco Backoffice.


Image property
--------------

Create a new DataType in the Developer section that uses "Chalmers.MediaContentUsage" (this is already done in the package). Add a property to your Image MediaType with this DataType. This will trigger the Property Editor and fetch relations to Content for the Image.


Re-indexing
-----------

When installed for the first time, the new RelationType is created and all published Content is searched for Media relations. If you add DataTypes via web.config or want to re-index the relations this can be done by deleting the RelationType in Umbraco Backoffice and restarting the Application Pool.


![alt text](https://raw.githubusercontent.com/rolfis/MediaContentUsage/master/MediaContentUsage.png)
