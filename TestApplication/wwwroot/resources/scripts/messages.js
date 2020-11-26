(function () {
    function isString(value) {
        return typeof value === 'string' || value instanceof String;
    }

    var extendMessage = function (container, key, value) {
        if (container == undefined || container == null) {
            container = {};
        }
        if (key.indexOf('.') > 0) {
            container[key.substring(0,key.indexOf('.'))] = extendMessage(container[key.substring(0,key.indexOf('.'))], key.substring(key.indexOf('.') + 1), value);
        } else {
            if (isString(value)) {
                container[key] = value;
            } else {
                for (var skey in value) {
                    container[key] = extendMessage(container[key], skey, value[skey]);
                }
            }
        }
        return container;
    };

    var messages = {};
    window.App = window.App || {};
    window.App.Messages = window.App.Messages || {};
    window.App.Messages.extend = function (language, message) {
        messages = extendMessage(messages, language, message);
    };
    window.App.Messages.Translate = function (message) {
        var language = window.navigator.userLanguage || window.navigator.language;
        if (language.indexOf('-') >= 0) {
            language = language.substring(0, language.indexOf('-'));
        }
        var splt = message.split('.');
        var ret = null;
        var langs = [language, 'en'];
        for (var x = 0; x < langs.length; x++) {
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
        return (ret == null || ret == undefined ? message : ret);
    };
}).call(this);