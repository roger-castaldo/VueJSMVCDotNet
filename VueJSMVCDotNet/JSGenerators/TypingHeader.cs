using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    internal class TypingHeader : IBasicJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, string modelNamespace, string urlBase)
        {
            builder.Append(@"const _numberRanges = {
    'Int16': { low:-32768, high:32767,hasDecimal:false},
    'Int32': { low: -2147483648, high: 2147483647,hasDecimal:false},
    'Int64': { low: BigInt('-9223372036854775808'), high: BigInt('9223372036854775807'),hasDecimal:false },
    'SByte': { low: -128, high: 127,hasDecimal:false },
    'Single': { low: Number('-3.402823e38'), high: Number('3.402823e38'),hasDecimal:true },
    'Decimal': { low: Number('-79228162514264337593543950335'), high: Number('79228162514264337593543950335'),hasDecimal:true },
    'Double': { low: Number('-1.7976931348623157E+308'), high: Number('1.7976931348623157E+308'),hasDecimal:true },
    'UInt16': { low: 0, high: 65535,hasDecimal:false},
    'UInt32': { low: 0, high: 4294967295,hasDecimal:false },
    'UInt64': { low: 0, high: BigInt('18446744073709551615'),hasDecimal:false },
    'Byte': { low: 0,high: 255,hasDecimal:false },
};

const _trueRegex = /^(t(rue)?|y(es)?|1)$/i;
const _falseRegex = /^(f(alse)?|n(o)?|0)$/i;
const _base64Regex = /^([0-9a-zA-Z+/]{4})*(([0-9a-zA-Z+/]{2}==)|([0-9a-zA-Z+/]{3}=))?$/;
const _ipv4Regex = /^(?!0)(?!.*\.$)((1?\d?\d|25[0-5]|2[0-4]\d)(\.|$)){4}$/;
const _ipv6Regex = /(([0-9a-fA-F]{1,4}:){7,7}[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,7}:|([0-9a-fA-F]{1,4}:){1,6}:[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,5}(:[0-9a-fA-F]{1,4}){1,2}|([0-9a-fA-F]{1,4}:){1,4}(:[0-9a-fA-F]{1,4}){1,3}|([0-9a-fA-F]{1,4}:){1,3}(:[0-9a-fA-F]{1,4}){1,4}|([0-9a-fA-F]{1,4}:){1,2}(:[0-9a-fA-F]{1,4}){1,5}|[0-9a-fA-F]{1,4}:((:[0-9a-fA-F]{1,4}){1,6})|:((:[0-9a-fA-F]{1,4}){1,7}|:)|fe80:(:[0-9a-fA-F]{0,4}){0,4}%[0-9a-zA-Z]{1,}|::(ffff(:0{1,4}){0,1}:){0,1}((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])|([0-9a-fA-F]{1,4}:){1,4}:((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9]))/gi;
const _versionRegex = /^([0-9]+)\.([0-9]+)(\.([0-9]+))?(\.([0-9]+))?$/;
const _guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

const _b64toBlob = function(b64Data, contentType, sliceSize) {
    contentType = contentType || '';
    sliceSize = sliceSize || 512;

    var byteCharacters = atob(b64Data);
    var byteArrays = [];

    for (var offset = 0; offset < byteCharacters.length; offset += sliceSize) {
        var slice = byteCharacters.slice(offset, offset + sliceSize);

        var byteNumbers = new Array(slice.length);
        for (var i = 0; i < slice.length; i++) {
            byteNumbers[i] = slice.charCodeAt(i);
        }

        var byteArray = new Uint8Array(byteNumbers);

        byteArrays.push(byteArray);
    }

    var blob = new Blob(byteArrays, { type: contentType });
    return blob;
};

const _checkDataType = function(type, value,enumlist) {
	if (type.indexOf('System.') === 0)
		type = type.substring(7);
	if (type.substring(type.length - 1) !== '?') {
		if (type !== 'Boolean') {
			if (value === null || value === undefined) {
				throw 'invalid type: Value is not allowed to be null';
			}
		} 
	}else {
		type = type.substring(0, type.length - 1);
		if (value === null || value === undefined) {
			return value;
		}
	}
	if (type.indexOf('[]') >= 0 && type!=='Byte[]') {
		if (!Array.isArray(value))
			throw 'invalid type: Value not an array';
		type = type.substring(0, type.length - 2);
		if (type.indexOf('[]') < 0) {
			type = type + '?';
        }
		for (var x = 0; x < value.length; x++) {
			try {
				value[x] = _checkDataType(type, value[x]);
			} catch (err) {
				throw 'invalid type: Value[' + x.toString() + '] is not of the type ' + type;
			}
		}
	} else { 
		switch (type) {
			case 'String':
				if (typeof value !== 'string' && value.toString === undefined)
					throw 'invalid type: Value not a string and cannot be converted';
				else if (typeof value !== 'string' && value.toString !== undefined){
					if (value.toString()==='[object Object]')
						throw 'invalid type: Value not a string and cannot be converted';	
					value = value.toString();
				}
				break;
			case 'Char':
				if (typeof value !== 'string' && value.toString === undefined)
					throw 'invalid type: Value not a char and cannot be converted';
				else if (typeof value !== 'string' && value.toString !== undefined)
					value = value.toString();
				if (value.length > 1 || value.length == 0)
					throw 'invalid type: Value not a char';
				break;
			case 'UInt64':
			case 'Int64':
				if (typeof value !== 'bigint' && isNaN(value))
					throw 'invalid type: Value not a number and cannot be converted';
				else if (typeof value !== 'bigint')
					value = BigInt(value);
			case 'Int16':
			case 'Int32':
			case 'SByte':
			case 'Single':
			case 'Decimal':
			case 'Double':
			case 'UInt16':
			case 'UInt32':
			case 'Byte':
				if (typeof value !== 'number' && typeof value !== 'bigint') {
					if (typeof value !== 'string' && value.toString === undefined)
						value = value.toString();
					if (typeof value === 'string') {
						try {
							value = Number(value);
						} catch (err) {
						}
					}
				}
				if (typeof value !== 'bigint' && isNaN(value))
					throw 'invalid type: Value not a number and cannot be converted';
				if (value < (typeof value === 'bigint' ? (typeof _numberRanges[type].low !== 'bigint' ? BigInt(_numberRanges[type].low) : _numberRanges[type].low) : _numberRanges[type].low) || value > (typeof value === 'bigint' ? (typeof _numberRanges[type].high !== 'bigint' ? BigInt(_numberRanges[type].high) : _numberRanges[type].high) : _numberRanges[type].high))
                    throw 'invalid type: Value is a number, but is too large for a '+type;
				if (!_numberRanges[type].hasDecimal && value.toString().indexOf('.')>=0)
					throw 'invalid type: Value is a number, but cannot has a decimal for '+type;
				break;
			case 'Boolean':
				if (value == null || value == undefined)
					value = false;
				else if (typeof value !== 'boolean') {
					if (typeof value !== 'string' && value.toString === undefined)
                        value = value.toString();
					if (_trueRegex.test(value))
						value = true;
					else if (_falseRegex.test(value))
						value = false;
					else
						throw 'invalid type: Value not boolean and cannot be converted';
                }
				break;
			case 'Enum':
                if (typeof value !== 'string' && value.toString === undefined)
					value = value.toString();
				if (enumlist !== undefined && enumlist !== null) {
					if (!enumlist.includes(value))
                        throw 'invalid type: Value is not in the list of enumarators';	
                }
				break;
			case 'DateTime':
				if (Object.prototype.toString.call(value) !== '[object Date]') {
					try {
						value = new Date(value);
						if (isNaN(value))
							throw '';
					} catch (err) {
						throw 'invalid type: Value is not a Date and cannot be converted to one';
                    }
                }
				break;
			case 'Byte[]':
				if (value.byteLenth === undefined) {
                    if (typeof value !== 'string' && value.toString === undefined)
						value = value.toString();
					if (!_base64Regex.test(value))
						throw 'invalid type: Value is not a Byte[] and cannot be converted to one';
					value = atob(value); //invert this is btoa
                }
				break;
			case 'Net.IPAddress':
                if (typeof value !== 'string' && value.toString === undefined)
					value = value.toString();
				if (!_ipv4Regex.test(value)) {
					if (!_ipv6Regex.test(value)) {
						throw 'invalid type: Value is not an IPAddress';
					}
				}
				break;
			case 'Version':
                if (typeof value !== 'string' && value.toString === undefined)
					value = value.toString();
				if (!_versionRegex.test(value))
                    throw 'invalid type: Value is not a Version';
				break;
			case 'Guid':
                if (typeof value !== 'string' && value.toString === undefined)
					value = value.toString();
				if (!_guidRegex.test(value))
                    throw 'invalid type: Value is not a Guid';
				break;
			case 'Exception':
				if (typeof value === 'string') {
					try {
						value = JSON.parse(value);
					} catch (err) { }
				}
                if (Object.prototype.toString.call(value) !== '[object Error]') {
					if (typeof value === 'object' && !Array.isArray(value)) {
						try {
							_checkDataType('String', value['Message']);
							_checkDataType('String', value['StackTrace']);
							_checkDataType('String', value['Source']);
						} catch (err) {
							throw 'invalid type: Value is not an Exception';
						}
					} else
						throw 'invalid type: Value is not an Exception';
				}
				break;
        }
    }
	return value;
};

const _checkProperty = function(name,type, value,enumlist) {
	try{
		return _checkDataType(type,value,enumlist);
	}catch(err){
		throw 'Cannot set '+name+': '+err;
	}
};

const _defineTypedObject = function(definition) {
	var target = {};
	for (var prop in definition)
		target[prop] = (definition[prop].initial !== undefined ? definition[prop].initial : null);
	return new Proxy(target, {
		set(target, property, value) {
			if (property === '_definition')
				throw 'Unable to alter definition of created object';
			if (definition[property] !== undefined) {
				value = _checkProperty(property,(definition[property].type == undefined ? definition[property] : definition[property].type), value, definition[property].enumlist);
			}
			target[property] = value;
		},
		get(target, property) {
			if (property === '_definition')
				return definition;
			return target[property];
        }
	});
};

const _stripBigInt = function(data){
	var ret = data;
	if (data!==null && data!==undefined){
		if (Array.isArray(data)){
			ret = [];
			for(var x=0;x<data.length;x++){
				ret.push(_stripBigInt(data[x]));
			}
		}
		else if (typeof data === 'object'){
			ret={};
			for(var prop in data){
				if (prop!=='_hashCode'){
					ret[prop] = _stripBigInt(data[prop]);
				}
			}
		}else if (typeof data ==='bigint'){
			ret = data.toString();
		}
	}
	return ret;
};
");
        }
    }
}
