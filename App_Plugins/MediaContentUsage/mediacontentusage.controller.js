angular.module("umbraco").controller("Chalmers.MediaContentUsageController", function ($scope, editorState, mediaContentUsageResource, notificationsService) {

    // Indicate that we are loading
    $scope.loading = true;

    // Get data about Content usage from our resource
    mediaContentUsageResource.getMediaContentUsage(editorState.current.id).then(function (response) {
        $scope.media = response.data;
        $scope.loading = false;
    });
});