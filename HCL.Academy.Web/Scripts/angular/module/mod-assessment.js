/// <reference path="mod-ingacademy.js" />
/// <reference path="../library/angular.min.js" />
//(function () {
//    var assessmentApp = angular.module("assessmentModule", []);
//    ingApp.Modules.assessmentModule = assessmentApp;
//}());
var assessmentApp = angular.module("assessmentModule", ['angular-growl']);
var fnAssessmentController = function ($scope, $http, growl, $window) {
    $scope.assessmentDetails = null;
    $scope.answers = [];

    $scope.finalSubmit = false;

    $scope.verify = function (index) {
        debugger;
        var question = $scope.answers[index].questionDetail;
        var correctOptionValue = question.CorrectOption;
         
        if ($scope.answers[index].selectedoption.trim() == correctOptionValue.trim()) {
            $scope.answers[index].isCorrect = true;
            $scope.answers[index].mark = parseInt(question.Marks);
        }
        else {
            $scope.answers[index].isCorrect = false;
        }
        $scope.answers[index].questionDetail.SelectedOption = $scope.answers[index].selectedoption;

    }


    $scope.showAll = function () {
        growl.warning('This is warning mesage.', { title: 'Warning!' });
        growl.error('This is error mesage.', { title: 'Error!' });
        growl.success('This is success mesage.', { title: 'Success!' });
        growl.info('This is an info mesage.', { title: 'Info!' });
    }

    $scope.getScore = function () {
        debugger;
        $window.onbeforeunload = function (e) {
            return null;
        };
        clearInterval($scope.interval);
        var answers = $scope.answers;
        var securedMarks = 0;
        for (var i = 0; i < answers.length; i++) {
            answers[i].cssClass = answers[i].isCorrect == true ? "quest-success" : "quest-unsuccess";
            securedMarks = securedMarks + parseInt(answers[i].mark);
        }

        ///Addedd
        var qDetails = [];
        for (var i = 0; i < answers.length; i++) {
            qDetails[i] = answers[i].questionDetail;          
        }
        ///

        var objAssesmentCompletion = {
            SecuredMarks: securedMarks,
            AssessmentId: $scope.assessmentDetails.AssessmentId,
            TotalMarks: $scope.assessmentDetails.TotalMarks,
            PassingPercentage: $scope.assessmentDetails.PassingPercentage            
        };
        getScorefromServer($http, $scope, objAssesmentCompletion, qDetails, growl);
        $scope.finalSubmit = true;
    };

    $scope.reset = function () {
        var answers = $scope.answers;
        $scope.finalSubmit = false;
        for (var i = 0; i < answers.length; i++) {
            answers[i].selectedoption = null;
            answers[i].mark = 0;
            answers[i].cssClass = "";
        }
    }

    getAssessments($http, $scope);

    $scope.timer = function (min) {
        $scope.minute = min;
        $scope.second = "00";
        $('#timeTicker').html($scope.minute + ':' + $scope.second);
        setTimeout(function () {
            $scope.minute = parseInt($scope.minute) - 1;
            $scope.minute = $scope.minute < 10 ? "0" + $scope.minute : $scope.minute;
            $scope.second = 59;
            $('#timeTicker').html($scope.minute + ':' + $scope.second);
            $scope.interval = setInterval(function () {
                var sec = parseInt($scope.second) == 0 ? 60 : parseInt($scope.second);
                sec = parseInt(sec) - 1;
                $scope.second = sec < 10 ? "0" + sec : sec;
                $('#timeTicker').html($scope.minute + ':' + $scope.second);

                if (sec == 0) {
                    var minute = parseInt($scope.minute);
                    minute = minute - 1;
                    if (minute == -1) {
                        clearInterval($scope.interval);
                        $scope.getScore();
                    }
                    else {
                        $scope.minute = parseInt($scope.minute) - 1;
                        $scope.minute = $scope.minute < 10 ? "0" + $scope.minute : $scope.minute;
                        $('#timeTicker').html($scope.minute + ':' + $scope.second);
                    }
                }

            }, 1000);
        }, 3000);
    }


    $window.onbeforeunload = function (e) {
        growl.error('You will lose an attempt');
        return "You will lose an attempt";
    };

};
assessmentApp.controller("assessmentCtrl", fnAssessmentController);


var getAssessments = function ($http, $scope) {
    var req = {
        method: 'GET',
        url: '/Assessment/AssessmentQuestions',
    }

    $http(req).then(function (obj) {
        //console.log(JSON.stringify(obj.data));
        if (obj.data != null) {
            $scope.assessmentDetails = obj.data;
            for (var i = 0; i < $scope.assessmentDetails.QuestionDetails.length; i++) {
                var answer = {
                    questionDetail: $scope.assessmentDetails.QuestionDetails[i],
                    selectedoption: null,
                    isCorrect: null,
                    cssClass: "",
                    mark: 0
                }
                $scope.answers.push(answer);
            }
            var trainingDetails = $scope.assessmentDetails.AssessmentDetails[0];
            $scope.timer(parseInt(trainingDetails.TrainingAssessmentTimeInMins));
        }
    },
    function () {
    });
}

var getScorefromServer = function ($http, $scope, objAssesmentCompletion,qDetails, growl) {

    var inputData = JSON.stringify({
        'result': objAssesmentCompletion,
        'QuestionDetails': qDetails
    });

    var req = {
        method: 'POST',
        url: '/Assessment/AssessmentResult',
        contentType: "application/json;",
        data: inputData
    }
    $http(req).then(function (obj) {
       // console.log(JSON.stringify(obj.data));
        if (obj.data) {
            $scope.resultStatus = true;
            growl.success('Your score is ' + objAssesmentCompletion.SecuredMarks, { title: 'Passed' });


        }
        else {
            $scope.resultStatus = false;
            growl.error('Your score is ' + objAssesmentCompletion.SecuredMarks, { title: 'Failed' });
        }
    },
   function () {
   });
}

function timeTicker() {
    var interval = null;
    $('#timeTicker').html("60:00");
    //document.getElementById('timeTicker').innerHTML = '30:00';
    setTimeout(function () {
        var min = 59;
        var sec = 60;
        interval = setInterval(function () {
            var now = new Date(); //get local time
            sec = parseInt(sec) - 1;
            $('#timeTicker').html(min + ':' + sec);
            //document.getElementById('timeTicker').innerHTML = min + ':' + sec;
            if (sec == 0) {
                $('#timeTicker').html(min + ':' + sec);
                //document.getElementById('timeTicker').innerHTML = min + ':' + sec;
                min = parseInt(min) - 1;
                sec = 60;
            }
            if (min == -1) {
                clearInterval(interval);
                getScore();
            }


        }, 1000);
    }, 200);
    $('.close').click(function () {
        $('#timeTicker').html("");
        clearInterval(interval);
    });
}