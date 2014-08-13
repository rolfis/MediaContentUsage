//adds the resource to umbraco.resources module:
angular.module('umbraco.resources').factory('mediaContentUsageResource',
    function ($q, $http) {
        return {
            getMediaContentUsage: function (id, dataTypeIds) {
                return $http({
                    url: "backoffice/CTH/MediaContentUsage/GetMediaContentUsage",
                    method: "GET",
                    params: { id: id, propertyTypes: dataTypeIds }
                });
            }
        };
    }
);
