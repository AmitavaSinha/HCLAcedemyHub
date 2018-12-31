
    $(document).ready(function () {

        $.ajax({
            type: "GET",
            url: "/Home/GetLearningJourney",             
            async: true,
            cache: true,
            success: function (response) {
                $("#MyLearningJourney").html(response);
            },
            failure: function (response) {
                alert(response.d);
            }
        });

        $.ajax({
            type: "GET",
            url: "/Home/GetAssessments",            
            async: true,
            cache: true,
            success: function (response) {
                $("#assesmentList").html(response);
                
                if ($('#assesmentInprogress >.itemRows').length > 0) {
                    GridPagination("#assesmentInprogress", "#assesmentInprogressPagination");
                }

                if ($('#assesmentCompleted >.itemRows').length > 0) {
                    GridPagination("#assesmentCompleted", "#assesmentCompletedPagination");
                }
              
                $('[data-toggle=confirmation]').confirmation({
                    rootSelector: '[data-toggle=confirmation]',
                    title: $("#confrmMsgBox").html(),
                    btnOkLabel: 'OK',
                    btnCancelLabel: 'Cancel',
                    onConfirm: function () { return true; },
                    onCancel: function () { return false; }
                });
            },
            failure: function (response) {
                alert(response.d);
            }
        });      

        $.ajax({
            type: "GET",
            url: "/Home/GetEventsItems",           
            async: true,
            cache: true,
            success: function (response) {

                $("#EventsList").html(response);

                $('.carousel').carousel({
                    pause: "false",
                    interval: 5000
                });
            },
            failure: function (response) {
                alert(response.d);
            }
        });

        $.ajax({
            type: "GET",
            url: "/Home/GetTrainings",           
            async: true,
            cache: true,
            success: function (response) {

                $("#trainings").html(response);

                if ($('#trainingMandatory >.itemRows').length > 0) {
                    GridPagination("#trainingMandatory", "#trainingMandatoryPagination");
                }

                if ($('#trainingSuggested >.itemRows').length > 0) {
                    GridPagination("#trainingSuggested", "#trainingSuggestedPagination");
                }
            },
            failure: function (response) {
                alert(response.d);
            }
        });

        //$.ajax({
        //    type: "GET",
        //    url: "/Home/GetNewsItems",
        //    async: true,
        //    cache: true,
        //    success: function (response) {

        //        $("#NewsList").html(response);

        //        $('.carousel').carousel({
        //            pause: "false",
        //            interval: 5000
        //        });
        //    },
        //    failure: function (response) {
        //        alert(response.d);
        //    }
        //});

        $.ajax({
            type: "GET",
            url: "/Home/GetRssNewsItems",
            async: true,
            cache: true,
            success: function (response) {

                $("#RssNewsList").html(response);

                $('.carousel').carousel({
                    pause: "false",
                    interval: 5000
                });
            },
            failure: function (response) {
                alert(response.d);
            }
        });
    });

function GridPagination(dataContainer, paginationContainer) {

    window.tp = new Pagination(paginationContainer, {
        itemsCount: $(dataContainer + ' >.itemRows').length,
        isDisabled: false,
        onPageSizeChange: function (ps) {
            console.log('changed to ' + ps);
        },
        onPageChange: function (paging) {
            //custom paging logic here
            console.log(paging);
            var start = paging.pageSize * (paging.currentPage - 1),
                end = start + paging.pageSize,
                $rows = $(dataContainer).find('.itemRows');

            $rows.hide();

            for (var i = start; i < end; i++) {
                $rows.eq(i).show();
            }
        }
    });
}

 