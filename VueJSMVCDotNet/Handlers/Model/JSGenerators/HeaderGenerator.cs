﻿using Org.Reddragonit.VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using static Org.Reddragonit.VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace Org.Reddragonit.VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class HeaderGenerator : IBasicJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, string urlBase, sModelType[] models)
        {
            builder.AppendLine(@"
const isString = function(value) {
    return typeof value === 'string' || value instanceof String;
};
const isFunction = function(obj) {
    if (obj === null) {
        return false;
    }
    return typeof obj == 'function' || false;
};
const _keys = function(obj) {
    if (!isObject(obj)) return [];
    if (Object.keys) return Object.keys(obj);
    let keys = [];
    for (let key in obj)
        if (has(obj, key)) keys.push(key);
    if (hasEnumBug) collectNonEnumProps(obj, keys);
    return keys;
};
const isDate = function(obj) {
    return Object.prototype.toString.call(obj) === '[object Date]';
};
const isObject = function(obj) {
    let type = typeof obj;
    return (type === 'function' || type === 'object' && !!obj) && !isDate(obj);
};
const cloneData = function(obj) {
    if (obj === null) {
        return null;
    }
    if (!isObject(obj) && !Array.isArray(obj)) {
        return obj;
    }
    if (Array.isArray(obj)) {
        let ret = [];
        for (let x = 0; x < obj.length; x++) {
            ret.push(cloneData(obj[x]));
        }
        return ret;
    } else {
        let ret = {};
        let props = Object.getOwnPropertyNames(obj);
        for (let x = 0; x < props.length; x++) {
            if (!isFunction(obj[props[x]])) {
                ret[props[x]] = cloneData(obj[props[x]]);
            }
        }
        return ret;
    }
}
const _dateRegex = new RegExp('^\\d{4}-((0[1-9])|(1[0-2]))-((0[1-9])|([12]\\d)|(3[01]))T([0-5]\\d):([0-5]\\d):([0-5]\\d).\\d{3}Z$');
const _fixDates = function(data) {
    if (data !== null) {
        if (Array.isArray(data)) {
            for (let x = 0; x < data.length; x++) {
                if (isString(data[x])) {
                    if (_dateRegex.test(data[x])) {
                        data[x] = new Date(data[x]);
                    }
                } else if (Array.isArray(data[x]) || isObject(data[x])) {
                    data[x] = _fixDates(data[x]);
                }
            }
        } else if (isObject(data)) {
            for (let prop in data) {
                data[prop] = _fixDates(data[prop]);
            }
        } else if (isString(data)) {
            if (_dateRegex.test(data)) {
                data = new Date(data);
            }
        }
    }
    return data;
};
const ajax = function(options) {
    if (options.isSlow !== undefined && options.isSlow) {
        return new Promise((resolve, reject) => {
            delete options.isSlow;
            let isArray = (options.isArray == undefined ? false : options.isArray);
            ajax(options).then(
                res => {
                    let ret = [];
                    let url = res.json();
                    let pullCall = function() {
                        ajax({
                            url: url,
                            method:'PULL',
                            useJSON: true
                        }).then(
                            res => {
                                res = res.json();
                                if (res.Data.length > 0) {
                                    Array.prototype.push.apply(ret, res.Data);
                                }
                                if (res.HasMore) {
                                    pullCall();
                                } else if (res.IsFinished) {
                                    resolve({
                                        json: function() {
                                            return (isArray ? ret : (ret.length == 0 ? null : ret[0]));
                                        }
                                    });
                                } else {
                                    setTimeout(pullCall, 200);
                                }
                            },
                            err => {
                                reject(err);
                            }
                        );
                    };
                    pullCall();
                },
                err => {
                    reject(err);
                }
            );
        });
    } else {
        return new Promise((resolve, reject) => {
            if (options.url === null || options.url===undefined || options.url==='') {
                throw 'Unable to call empty url';
            }
            options = Object.assign({},{
                method:'GET',
                credentials: false,
                body: null,
                credentials:'include',
                headers: {
                },
                data: null,
                url: null,
                useJSON: true
            },options);
            if (options.useJSON) {
                options.headers['Content-Type'] = 'application/json';
            }
            options.url += (options.url.indexOf('?') === -1 ? '?' : '&') + '_=' + parseInt((new Date().getTime() / 1000).toFixed(0)).toString();
			let data = null;
            if (options.data !== null) {
                if (options.useJSON) {
                    data = JSON.stringify(_stripBigInt(options.data));
                } else {
                    data = new FormData();
                    for (let prop in options.data) {
                        if (Array.isArray(options.data[prop])) {
                            if (options.data[prop].length > 0) {
                                if (isObject(options.data[prop][0])) {
                                    for (let x = 0; x < options.data[prop].length; x++) {
                                        data.append(prop + ':json', JSON.stringify(options.data[prop][x]));
                                    }
                                } else {
                                    for (let x = 0; x < options.data[prop].length; x++) {
                                        data.append(prop, options.data[prop][x]);
                                    }
                                }
                            }
                        } else if (isObject(options.data[prop])) {
                            data.append(prop + ':json', JSON.stringify(options.data[prop]));
                        } else {
                            data.append(prop, options.data[prop]);
                        }
                    }
                }
            }
            if (options.method!=='GET'){
			    options.body=data;
            }else{
                delete options.body;
            }
			let url = options.url;
			delete options.url;
			fetch(url,options).then(
				response=>{
					response.text().then(content=>{
						if (response.ok){
							resolve({
								ok:true,
								text:function(){return content;},
								json:function(){return (response.headers.get('Content-Type')==='text/text' ? content : _fixDates(JSON.parse(content)));}
							});
						}else{
							reject({
								ok: false,
								text: function() { return content; }
							});
						}
					});
				},
				rejected=>{
					reject({
						ok:false,
						text:function(){return rejected;}
					});
				}
			);
        });
    }
};

/*borrowed from undescore source*/
const has = function(obj, path) {
    return obj !== null && Object.prototype.hasOwnProperty.call(obj, path);
}
let eq, deepEq;
eq = function(a, b, aStack, bStack) {
    // Identical objects are equal. `0 === -0`, but they aren't identical.
    // See the [Harmony `egal` proposal](http://wiki.ecmascript.org/doku.php?id=harmony:egal).
    if (a === b) return a !== 0 || 1 / a === 1 / b;
    // `null` or `undefined` only equal to itself (strict comparison).
    if (a == null || b == null) return false;
    // `NaN`s are equivalent, but non-reflexive.
    if (a !== a) return b !== b;
    // Exhaust primitive checks
    let type = typeof a;
    if (type !== 'function' && type !== 'object' && typeof b !== 'object') return false;
    return deepEq(a, b, aStack, bStack);
};

// Internal recursive comparison function for `isEqual`.
deepEq = function(a, b, aStack, bStack) {
    // Compare `[[Class]]` names.
    let className = toString.call(a);
    if (className !== toString.call(b)) return false;
    switch (className) {
        // Strings, numbers, regular expressions, dates, and booleans are compared by value.
        case '[object RegExp]':
            // RegExps are coerced to strings for comparison (Note: '' + /a/i === '/a/i')
        case '[object String]':
            // Primitives and their corresponding object wrappers are equivalent; thus, `""5""` is
            // equivalent to `new String(""5"")`.
            return '' + a === '' + b;
        case '[object Number]':
            // `NaN`s are equivalent, but non-reflexive.
            // Object(NaN) is equivalent to NaN.
            if (+a !== +a) return +b !== +b;
            // An `egal` comparison is performed for other numeric values.
            return +a === 0 ? 1 / +a === 1 / b : +a === +b;
        case '[object Date]':
        case '[object Boolean]':
            // Coerce dates and booleans to numeric primitive values. Dates are compared by their
            // millisecond representations. Note that invalid dates with millisecond representations
            // of `NaN` are not equivalent.
            return +a === +b;
        case '[object Symbol]':
            return SymbolProto.valueOf.call(a) === SymbolProto.valueOf.call(b);
    }

    let areArrays = className === '[object Array]';
    if (!areArrays) {
        if (typeof a !== 'object' || typeof b !== 'object') return false;

        // Objects with different constructors are not equivalent, but `Object`s or `Array`s
        // from different frames are.
        let aCtor = a.constructor,
            bCtor = b.constructor;
        if (aCtor !== bCtor && !(isFunction(aCtor) && aCtor instanceof aCtor &&
                isFunction(bCtor) && bCtor instanceof bCtor) &&
            ('constructor' in a && 'constructor' in b)) {
            return false;
        }
    }
    // Assume equality for cyclic structures. The algorithm for detecting cyclic
    // structures is adapted from ES 5.1 section 15.12.3, abstract operation `JO`.

    // Initializing stack of traversed objects.
    // It's done here since we only need them for objects and arrays comparison.
    aStack = aStack || [];
    bStack = bStack || [];
    let length = aStack.length;
    while (length--) {
        // Linear search. Performance is inversely proportional to the number of
        // unique nested structures.
        if (aStack[length] === a) return bStack[length] === b;
    }

    // Add the first object to the stack of traversed objects.
    aStack.push(a);
    bStack.push(b);

    // Recursively compare objects and arrays.
    if (areArrays) {
        // Compare array lengths to determine if a deep comparison is necessary.
        length = a.length;
        if (length !== b.length) return false;
        // Deep compare the contents, ignoring non-numeric properties.
        while (length--) {
            if (!eq(a[length], b[length], aStack, bStack)) return false;
        }
    } else {
        // Deep compare objects.
        let keys = _keys(a),
            key;
        length = keys.length;
        // Ensure that both objects contain the same number of properties before comparing deep equality.
        if (_keys(b).length !== length) return false;
        while (length--) {
            // Deep compare each member
            key = keys[length];
            if (!(has(b, key) && eq(a[key], b[key], aStack, bStack))) return false;
        }
    }
    // Remove the first object from the stack of traversed objects.
    aStack.pop();
    bStack.pop();
    return true;
};

// Perform a deep comparison to check if two objects are equal.
const isEqual = function(a, b) {
    if (Array.isArray(a) || Array.isArray(b)) {
        return deepEq(a, b);
    }
    return eq(a, b);
};");
        }
    }
}