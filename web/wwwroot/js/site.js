(function () {
    //Page ready
    $(function () {
        SetupBtnNoNavigate();
    })

    function SetupBtnNoNavigate() {
        $(".btn-no-navigate").each(function () {
            $(this).on("click", function (event) {
                event.preventDefault();
                let url = $(this).data("url");
                $.get(url);
            });
        });
    }
})();