/// <reference path="../library/angular.min.js" />
/// <reference path="../module/mod-ingacademy.js" />
//(function () {
//    var assessmentApp = ingApp.Modules.assessmentModule;
//    var fnAssessmentController = function ($scope) {
//        $scope.message = "Hello World";
//    };
//    assessmentApp.controller("assessmentCtrl", fnAssessmentController)
//}());
var fnAssessmentController = function ($scope) {
    $scope.message = "Hello World";
};
assessmentApp.controller("assessmentCtrl", fnAssessmentController)