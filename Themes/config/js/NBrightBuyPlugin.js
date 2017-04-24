$(document).ready(function () {

    $('.processing').show();

    $('a[id*="_cmdExport_"]').hide();

    NBrightBuyReport_nbxget('getreportlist', '#selectparams', '.reportlist');

    $('#cmdAdd').click(function () {
        $('.processing').show();
        NBrightBuyReport_nbxget('addreport', '#selectparams', '.reportlist');
    });

    $('#addnew').click(function () {
        $('.processing').show();
        $('#newitem').val('new');
        $('#selecteditemid').val('');
        $('.resultsrow').hide();
        //NBrightBuyReport_nbxget('addreport', '#selectparams', '.reportlist');
        NBrightBuyReport_nbxget('addnew', '#selectparams', '.reportlist');
    });

    $('.actionbuttonwrapper #cmdsave').click(function () {
        NBrightBuyReport_nbxget('savedata', '#editdata');
    });

    $('.actionbuttonwrapper #cmddelete').click(function () {
        if (confirm($('#deletemsg').val())) {
            NBrightBuyReport_nbxget('deletereport', '.reportlist', '.reportlist');
        }
    });

    $('.actionbuttonwrapper #cmdreturn').click(function () {
        $('#selecteditemid').val(''); // clear sleecteditemid.        
        NBrightBuyReport_nbxget('getreportlist', '#selectparams', '.reportlist');

    });

    $('#cmdRun').click(function () {
        $('.processing').show();
        $('.resultspane').val('');
        NBrightBuyReport_nbxget('runreport', '.reportlist', '.resultspane');
        $('#cmdSave').hide();
        $('#cmdDelete').hide();
        $('#cmdReturn').show();
        $('#cmdAdd').hide();
        $('.resultsrow').show();
    });

    $('#cmdExport').click(function () {
        $('.processing').show();
        $('.resultspane').val('');
        NBrightBuyReport_nbxget('exportreport', '.reportlist', '.reportlist');
        $('#cmdSave').hide();
        $('#cmdDelete').hide();
        $('#cmdReturn').show();
        $('#cmdAdd').hide();
        $('.resultsrow').show();
    });

    $('#importshow').click(function () {
        $('.importreportdiv').show();
        $('.reportlist').hide();
        $('#cmdAdd').hide();
        $('#cmdreturn').show();
    });


    // set the default edit language to the current langauge
    $('#editlang').val($('#selectparams #lang').val());

    // get list of records via ajax:  NBrightRazorTemplate_nbxget({command}, {div of data passed to server}, {return html to this div} )
    //NBrightBuyReport_nbxget('getdata', '#selectparams', '#editdata');
    //FIRST CALL TO API DONE IN Admin.cshtml

  /*   DONE IN Admin.cshtml

    $('.actionbuttonwrapper #cmdsave').click(function () {
        NBrightBuyReport_nbxget('savedata', '#editdata');
    });

   /* $('.actionbuttonwrapper #cmdreturn').click(function () {
        $('#selecteditemid').val(''); // clear sleecteditemid.        
        NBrightBuyReport_nbxget('getdata', '#selectparams', '#editdata');
    });

    $('.actionbuttonwrapper #cmddelete').click(function () {
        if (confirm($('#deletemsg').val())) {
            NBrightBuyReport_nbxget('deleterecord', '#editdata');
        }
    });

    $('#addnew').click(function () {
        $('.processing').show();
        $('#newitem').val('new');
        $('#selecteditemid').val('');
        NBrightBuyReport_nbxget('addnew', '#selectparams', '#editdata');
    });

    */
    $('.selecteditlanguage').click(function () {
        $('#selectlang').val($(this).attr('lang')); // alter lang after, so we get correct data record
        NBrightBuyReport_nbxget('selectlang', '#editdata', '#editdata'); // do ajax call to save current edit form
    });


});

$(document).on("NBrightBuyReport_nbxgetcompleted", NBrightBuyReport_nbxgetCompleted); // assign a completed event for the ajax calls


function NBrightBuyReport_nbxget(cmd, selformdiv, target, selformitemdiv, appendreturn) {
    $('.processing').show();

    $.ajaxSetup({ cache: false });

    var cmdupdate = '/DesktopModules/NBright/NBrightBuyReport/XmlConnector.ashx?cmd=' + cmd;
    var values = '';
    if (selformitemdiv == null) {
        values = $.fn.genxmlajax(selformdiv);
    }
    else {
        values = $.fn.genxmlajaxitems(selformdiv, selformitemdiv);
    }
    var request = $.ajax({
        type: "POST",
        url: cmdupdate,
        cache: false,
        data: { inputxml: encodeURI(values) }
    });

    request.done(function (data) {
        if (data != 'noaction') {
            if (appendreturn == null) {
                $(target).children().remove();
                $(target).html(data).trigger('change');
            } else
                $(target).append(data).trigger('change');

            $.event.trigger({
                type: "NBrightBuyReport_nbxgetcompleted",
                cmd: cmd
            });
        }
        //if (cmd == 'getdata' || cmd == 'getreportlist') { // only hide on getdata
            $('.processing').hide();
        //}
    });

    request.fail(function (jqXHR, textStatus) {
        alert("Request failed: " + textStatus);
    });
}



function NBrightBuyReport_nbxgetCompleted(e) {

    $('#selectlang').val("");

    if (e.cmd == 'addnew') {
        $('#newitem').val(''); // clear item so if new was just created we don;t create another record
        $('#selecteditemid').val($('#itemid').val()); // move the itemid into the selecteditemid, so page knows what itemid is being edited
        NBrightBuyReport_DetailButtons();
        $('.processing').hide(); // hide on add, not hidden by return
    }

    if (e.cmd == 'deleterecord') {
        $('#selecteditemid').val(''); // clear selecteditemid, it now doesn;t exists.
        //NBrightBuyReport_nbxget('getdata', '#selectparams', '#editdata');// relist after delete
        NBrightBuyReport_nbxget('getreportlist', '#selectparams', '.reportlist');
    }

    if (e.cmd == 'savedata') {
        $('#selecteditemid').val(''); // clear sleecteditemid.        
        //NBrightBuyReport_nbxget('getdata', '#selectparams', '#editdata');// relist after save
        NBrightBuyReport_nbxget('getreportlist', '#selectparams', '.reportlist');
    }

    // check if we are displaying a list or the detail and do processing.
    if (($('#selecteditemid').val() != '') || (e.cmd == 'addnew')) {
        // PROCESS DETAIL
        NBrightBuyReport_DetailButtons();

        if ($('input:radio[name=typeselectradio]:checked').val() == "cat") {
            $('.catdisplay').show();
            $('.propdisplay').hide();
        } else {
            $('.catdisplay').hide();
            $('.propdisplay').show();
        }

        $('input:radio[name=typeselectradio]').change(function () {
            if ($(this).val() == 'cat') {
                $('.catdisplay').show();
                $('.propdisplay').hide();
            } else {
                $('.catdisplay').hide();
                $('.propdisplay').show();
            }
        });

        if ($('input:radio[name=applydiscounttoradio]:checked').val() == "1") {
            $('.applyproperty').hide();
        } else {
            $('.applyproperty').show();
        }

        $('input:radio[name=applydiscounttoradio]').change(function () {
            if ($(this).val() == '1') {
                $('.applyproperty').hide();
            } else {
                $('.applyproperty').show();
            }
        });

        if ($('.applydaterangechk').is(":checked")) {
            $('.applydaterange').show();
        }

        $('.applydaterangechk').change(function () {
            if ($(this).is(":checked")) {
                $('.applydaterange').show();
            } else {
                $('.applydaterange').hide();
            }
        });

        $('#cmdrecalcpromo').click(function () {
            if (confirm($('#deletemsg').val())) {
                $('.processing').show();
                NBrightBuyReport_nbxget('recalc', '#selectparams', '#editdata'); // do ajax call to get edit form
            }
        });

    } else {
        //PROCESS LIST
        NBrightBuyReport_ListButtons();
        $('.edititem').unbind();
        $('.edititem').click(function () {
            $('.processing').show();
            $('#selecteditemid').val($(this).attr("itemid")); // assign the selected itemid, so the server knows what item is being edited
            NBrightBuyReport_nbxget('editreport', '#selectparams', '#editdata'); // do ajax call to get edit form
        });
        $(".catdisplay").prop("disabled", true);
        $(".propdisplay").prop("disabled", true);
    }
}

function NBrightBuyReport_DetailButtons() {
    $('#cmdsave').show();
    $('#cmddelete').show();
    $('#cmdreturn').show();
    $('#addnew').hide();
    $('input[datatype="date"]').datepicker(); // assign datepicker to any ajax loaded fields
}

function NBrightBuyReport_ListButtons() {
    $('#cmdsave').hide();
    $('#cmddelete').hide();
    $('#cmdreturn').hide();
    $('#addnew').show();
    $(".selecteditlanguage").hide();
}


