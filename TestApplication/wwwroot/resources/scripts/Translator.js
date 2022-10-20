const _format = function (str, args) {
    if (args === undefined || args === null) {
        return str;
    }
    return str.replace(/{(\d+)}/g, function (match, number) {
        return (typeof args[number] != 'undefined' ? (args[number] == null ? '' : args[number]) : '');
    });
};

let messages = {
    en: {
        "Name": "Name",
        "DOB": "Date Of Birth",
        "Registered": "Registered",
        "CurrentGrade": "Current Grade",
        "Details": "Details",
        "Filtering": "Filtering...",
        "Dashboard": "Dashboard",
        "Addresses": "Addresses",
        "Search": "Search",
        "Formatted":"This is a formatted message {0} was the argument"
    },
    fr: {
        "Name":"Nome"
    }
};

export default function (message,args,language) {
    language = (language===undefined ? window.navigator.userLanguage || window.navigator.language : language);
    if (language.indexOf('-') >= 0) {
        language = language.substring(0, language.indexOf('-'));
    }
    let splt = message.split('.');
    let ret = null;
    let langs = [language, 'en'];
    for (let x = 0; x < langs.length; x++) {
        ret = messages[langs[x]];
        var idx = 0;
        while (ret != undefined && ret != null) {
            ret = ret[splt[idx]];
            idx++;
            if (idx >= splt.length) {
                break;
            }
        }
        if (ret != undefined && ret != null) {
            break;
        }
    }
    return (ret == null || ret == undefined ? message : _format(ret,args));
}