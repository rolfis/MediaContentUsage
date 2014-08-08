angular.module("umbraco").controller("CTH.MediaContentUsageController", function ($scope, editorState, mediaContentUsageResource, notificationsService) {

        mediaContentUsageResource.getMediaContentUsage(editorState.current.id).then(function (response) {
        $scope.media = response.data;
    });
});