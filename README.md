GeomToJSON
==========

CLR function for SQLServer to Serialize Geometries To ESRI's ArcGIS REST API JSON Format.

For a discussion ESRI's ArcGIS REST API JSON Format see:
http://resources.arcgis.com/en/help/arcgis-rest-api/index.html#/Geometry_objects/02r3000000n1000000/

I am still trying to figure all this out.  But I have successfully published this to our database server
After publishing the function needs to be altered to allow for the large size of the serialized text.
I changed the return to a nvarchar of maximum size.

I am using the results of this function to create geometries in a graphics layer for an ESRI ArcGISRuntime .NET Project.
The text generated is passed to the ESRI.ArcGISRuntime.Geometry.Geometry.FromJSON() method.  Originally I serialized the
geometry in a TSQL function but performance was poor.  The CLR function performs much better. 
Note: curves are transformed to LineStrings before being serialized to JSON.

