(function($) {
    var utility = {
        //new MvcHtmlString(Model.JSON)
        prettyPrintJson: function(jsonString, elementId) {
            var str = JSON.stringify(jsonString, undefined, 2); // indentation level = 2
            $('#' + elementId).html(str);
            console.log(str);
        }
    };

    return utility;

})(jQuery);