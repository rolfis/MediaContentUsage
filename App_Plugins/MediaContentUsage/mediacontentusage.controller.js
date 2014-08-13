angular.module("umbraco").controller("CTH.MediaContentUsageController", function ($scope, editorState, mediaContentUsageResource, notificationsService) {

    // Indicate that we are loading
    $scope.loading = true;
    
    // Data Type Ids to search
    var dataTypeIds = $scope.model.config.dataTypeIds && $scope.model.config.dataTypeIds !== '0' ? $scope.model.config.dataTypeIds : "";

    // Get data about Content usage from our resource
    mediaContentUsageResource.getMediaContentUsage(editorState.current.id, dataTypeIds).then(function (response) {
		$scope.media = response.data;
		$scope.loading = false;
	});
});
