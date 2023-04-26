//START:HeaderGenerator
const isString = function (value) {
	return typeof value === 'string' || value instanceof String;
};

const isFunction = function (obj) {
	if (obj === null) {
		return false;
	}
	return typeof obj == 'function' || false;
};

const _keys = function (obj) {
	if (!_isObject(obj)) return [];
	if (Object.keys) return Object.keys(obj);
	let keys = [];
	for (let key in obj)
		if (_has(obj, key)) keys.push(key);
	if (hasEnumBug) collectNonEnumProps(obj, keys);
	return keys;
};

const _isDate = function (obj) {
	return Object.prototype.toString.call(obj) === '[object Date]';
};

const _isObject = function (obj) {
	let type = typeof obj;
	return (type === 'function' || type === 'object' && !!obj) && !_isDate(obj);
};

const cloneData = function (obj) {
	if (obj === null) {
		return null;
	}
	if (!_isObject(obj) && !Array.isArray(obj)) {
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
};

const _dateRegex = new RegExp('^\\d{4}-((0[1-9])|(1[0-2]))-((0[1-9])|([12]\\d)|(3[01]))T([0-5]\\d):([0-5]\\d):([0-5]\\d).\\d{3}Z$');

const _fixDates = function (data) {
	if (data !== null) {
		if (Array.isArray(data)) {
			for (let x = 0; x < data.length; x++) {
				if (isString(data[x])) {
					if (_dateRegex.test(data[x])) {
						data[x] = new Date(data[x]);
					}
				} else if (Array.isArray(data[x]) || _isObject(data[x])) {
					data[x] = _fixDates(data[x]);
				}
			}
		} else if (_isObject(data)) {
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

const ajax = function (options) {
	if (options.isSlow !== undefined && options.isSlow) {
		return new Promise((resolve, reject) => {
			delete options.isSlow;
			let isArray = (options.isArray == undefined ? false : options.isArray);
			ajax(options).then(
				res => {
					let ret = [];
					let url = res.json();
					let pullCall = function () {
						ajax({
							url: url,
							method: 'PULL',
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
										json: function () {
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
			if (options.url === null || options.url === undefined || options.url === '') {
				throw 'Unable to call empty url';
			}
			options = Object.assign({}, {
				method: 'GET',
				credentials: false,
				body: null,
				credentials: 'include',
				headers: {
				},
				data: null,
				url: null,
				useJSON: true
			}, options);
			if (options.useJSON) {
				options.headers['Content-Type'] = 'application/json';
			}
			for (let prop in securityHeaders) {
				if (options.headers[prop] === undefined)
					options.headers[prop] = securityHeaders[prop];
			}
			options.url += (options.url.indexOf('?') === -1 ? '?' : '&') + '_=' + parseInt((new Date().getTime() / 1000).toFixed(0)).toString();
			let data = null;
			if (options.data !== null) {
				if (options.useJSON) {
					data = JSON.stringify(stripBigInt(options.data));
				} else {
					data = new FormData();
					for (let prop in options.data) {
						if (Array.isArray(options.data[prop])) {
							if (options.data[prop].length > 0) {
								if (_isObject(options.data[prop][0])) {
									for (let x = 0; x < options.data[prop].length; x++) {
										data.append(prop + ':json', JSON.stringify(options.data[prop][x]));
									}
								} else {
									for (let x = 0; x < options.data[prop].length; x++) {
										data.append(prop, options.data[prop][x]);
									}
								}
							}
						} else if (_isObject(options.data[prop])) {
							data.append(prop + ':json', JSON.stringify(options.data[prop]));
						} else {
							data.append(prop, options.data[prop]);
						}
					}
				}
			}
			if (options.method !== 'GET') {
				options.body = data;
			} else {
				delete options.body;
			}
			let url = options.url;
			delete options.url;
			fetch(url, options).then(
				response => {
					for (let prop in securityHeaders) {
						if (response.headers[prop] !== undefined)
							securityHeaders[prop] = response.headers[prop];
					}
					response.text().then(content => {
						if (response.ok) {
							resolve({
								ok: true,
								text: function () { return content; },
								json: function () { return (response.headers.get('Content-Type') === 'text/text' ? content : _fixDates(JSON.parse(content))); }
							});
						} else {
							reject({
								ok: false,
								text: function () { return content; }
							});
						}
					});
				},
				rejected => {
					reject({
						ok: false,
						text: function () { return rejected; }
					});
				}
			);
		});
	}
};

/*borrowed from undescore source*/
const _has = function (obj, path) {
	return obj !== null && Object.prototype.hasOwnProperty.call(obj, path);
};

let _eq, _deepEq;

_eq = function (a, b, aStack, bStack) {
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
	return _deepEq(a, b, aStack, bStack);
};

// Internal recursive comparison function for `isEqual`.
_deepEq = function (a, b, aStack, bStack) {
	// Compare `[[Class]]` names.
	let className = toString.call(a);
	if (className !== toString.call(b)) return false;
	switch (className) {
		// Strings, numbers, regular expressions, dates, and booleans are compared by value.
		case '[object RegExp]':
		// RegExps are coerced to strings for comparison (Note: '' + /a/i === '/a/i')
		case '[object String]':
			// Primitives and their corresponding object wrappers are equivalent; thus, `"5"` is
			// equivalent to `new String("5")`.
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
			if (!_eq(a[length], b[length], aStack, bStack)) return false;
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
			if (!(_has(b, key) && _eq(a[key], b[key], aStack, bStack))) return false;
		}
	}
	// Remove the first object from the stack of traversed objects.
	aStack.pop();
	bStack.pop();
	return true;
};

// Perform a deep comparison to check if two objects are equal.
const isEqual = function (a, b) {
	if (Array.isArray(a) || Array.isArray(b)) {
		return _deepEq(a, b);
	}
	return _eq(a, b);
};

const _numberRanges = {
	'Int16': { low: -32768, high: 32767, hasDecimal: false },
	'Int32': { low: -2147483648, high: 2147483647, hasDecimal: false },
	'Int64': { low: BigInt('-9223372036854775808'), high: BigInt('9223372036854775807'), hasDecimal: false },
	'SByte': { low: -128, high: 127, hasDecimal: false },
	'Single': { low: Number('-3.402823e38'), high: Number('3.402823e38'), hasDecimal: true },
	'Decimal': { low: Number('-79228162514264337593543950335'), high: Number('79228162514264337593543950335'), hasDecimal: true },
	'Double': { low: Number('-1.7976931348623157E+308'), high: Number('1.7976931348623157E+308'), hasDecimal: true },
	'UInt16': { low: 0, high: 65535, hasDecimal: false },
	'UInt32': { low: 0, high: 4294967295, hasDecimal: false },
	'UInt64': { low: 0, high: BigInt('18446744073709551615'), hasDecimal: false },
	'Byte': { low: 0, high: 255, hasDecimal: false },
};

const _trueRegex = /^(t(rue)?|y(es)?|1)$/i;
const _falseRegex = /^(f(alse)?|n(o)?|0)$/i;
const _base64Regex = /^([0-9a-zA-Z+/]{4})*(([0-9a-zA-Z+/]{2}==)|([0-9a-zA-Z+/]{3}=))?$/;
const _ipv4Regex = /^(?!0)(?!.*\.$)((1?\d?\d|25[0-5]|2[0-4]\d)(\.|$)){4}$/;
const _ipv6Regex = /(([0-9a-fA-F]{1,4}:){7,7}[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,7}:|([0-9a-fA-F]{1,4}:){1,6}:[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,5}(:[0-9a-fA-F]{1,4}){1,2}|([0-9a-fA-F]{1,4}:){1,4}(:[0-9a-fA-F]{1,4}){1,3}|([0-9a-fA-F]{1,4}:){1,3}(:[0-9a-fA-F]{1,4}){1,4}|([0-9a-fA-F]{1,4}:){1,2}(:[0-9a-fA-F]{1,4}){1,5}|[0-9a-fA-F]{1,4}:((:[0-9a-fA-F]{1,4}){1,6})|:((:[0-9a-fA-F]{1,4}){1,7}|:)|fe80:(:[0-9a-fA-F]{0,4}){0,4}%[0-9a-zA-Z]{1,}|::(ffff(:0{1,4}){0,1}:){0,1}((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])|([0-9a-fA-F]{1,4}:){1,4}:((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9]))/gi;
const _versionRegex = /^([0-9]+)\.([0-9]+)(\.([0-9]+))?(\.([0-9]+))?$/;
const _guidRegex = /^(?:\{{0,1}(?:[0-9a-fA-F]){8}-(?:[0-9a-fA-F]){4}-(?:[0-9a-fA-F]){4}-(?:[0-9a-fA-F]){4}-(?:[0-9a-fA-F]){12}\}{0,1})$/;

const _b64toBlob = function (b64Data, contentType, sliceSize) {
	contentType = contentType || '';
	sliceSize = sliceSize || 512;

	let byteCharacters = atob(b64Data);
	let byteArrays = [];

	for (let offset = 0; offset < byteCharacters.length; offset += sliceSize) {
		let slice = byteCharacters.slice(offset, offset + sliceSize);

		let byteNumbers = new Array(slice.length);
		for (let i = 0; i < slice.length; i++) {
			byteNumbers[i] = slice.charCodeAt(i);
		}

		let byteArray = new Uint8Array(byteNumbers);

		byteArrays.push(byteArray);
	}

	let blob = new Blob(byteArrays, { type: contentType });
	return blob;
};

const _checkDataType = function (type, value, enumlist) {
	if (type.indexOf('System.') === 0)
		type = type.substring(7);
	if (type.substring(type.length - 1) !== '?') {
		if (type !== 'Boolean') {
			if (value === null || value === undefined) {
				throw 'invalid type: Value is not allowed to be null';
			}
		}
	} else {
		type = type.substring(0, type.length - 1);
		if (value === null || value === undefined) {
			return value;
		}
	}
	if (type.indexOf('[]') >= 0 && type !== 'Byte[]') {
		if (!Array.isArray(value))
			throw 'invalid type: Value not an array';
		type = type.substring(0, type.length - 2);
		if (type.indexOf('[]') < 0) {
			type = type + '?';
		}
		for (let x = 0; x < value.length; x++) {
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
				else if (typeof value !== 'string' && value.toString !== undefined) {
					if (value.toString() === '[object Object]')
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
					throw 'invalid type: Value is a number, but is too large for a ' + type;
				if (!_numberRanges[type].hasDecimal && value.toString().indexOf('.') >= 0)
					throw 'invalid type: Value is a number, but cannot has a decimal for ' + type;
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
			case 'IPAddress':
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
				if (Object.prototype.toString.call(value) !== '[object String]') {
					if (typeof value === 'object' && !Array.isArray(value)) {
						try {
							_checkDataType('String', value.Message);
							_checkDataType('String', value.StackTrace);
							_checkDataType('String', value.Source);
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

const checkProperty = function (name, type, value, enumlist) {
	try {
		return _checkDataType(type, value, enumlist);
	} catch (err) {
		throw 'Cannot set ' + name + ': ' + err;
	}
};

const stripBigInt = function (data) {
	let ret = data;
	if (data !== null && data !== undefined) {
		if (Object.prototype.toString.call(data) !== '[object Date]') {
			if (Array.isArray(data)) {
				ret = [];
				for (let x = 0; x < data.length; x++) {
					ret.push(stripBigInt(data[x]));
				}
			} else if (typeof data === 'bigint') {
				ret = data.toString();
			} else if (typeof data === 'object') {
				ret = {};
				for (let prop in data) {
					if (prop !== '_hashCode') {
						ret[prop] = stripBigInt(data[prop]);
					}
				}
			}
		}
	}
	return ret;
};

class EventHandler {
	#events;
	constructor(events) {
		this.#events = {};
		for (let i in events) {
			this.#events[events[i]] = [];
		}
	}

	on(event, callback) {
		if (this.#events[event] === undefined) { throw 'undefined event'; }
		this.#events[event].push(callback);
	}

	off(callback) {
		for (let prop in this.#events) {
			for (let x = 0; x < this.#events[prop].length; x++) {
				if (this.#events[prop][x] === callback) {
					this.#events[prop].splice(x, 1);
					break;
				}
			}
		}
	}

	trigger(event, data) {
		if (this.#events[event] === undefined) { throw 'undefined event'; }
		for (let i in this.#events[event]) {
			this.#events[event][i](data);
		}
	}
};

class ModelList {
	#events;
	#data;
	#isPaged;
	#totalPages = undefined;
	#constructModel;
	#url;
	#useGet;
	#params = undefined;
	#setParameters = undefined;
	#currentIndex = undefined;
	#currentPageSize = undefined;
	#pageVariableNames = undefined;

	get #currentPage() {
		return (!this.#isPaged ? undefined : Math.floor(this.#currentIndex / this.#currentPageSize));
	};


	#moveToPage(pageNumber) {
		if (pageNumber >= this.#totalPages || pageNumber < 0) {
			throw 'Unable to move to Page that is outside the page range.';
		} else {
			this.#currentIndex = pageNumber * this.#currentPageSize;
			return this.#reload();
		}
	};

	#moveToNextPage() {
		if ((this.#currentPage + 1) < this.#totalPages) {
			return this.#moveToPage(this.#currentPage + 1);
		} else {
			throw 'Unable to move to next Page as that will excess current total pages.';
		}
	};

	#moveToPreviousPage() {
		if ((this.#currentPage) >= 0) {
			return this.moveToPage(this.#currentPage - 1);
		} else {
			throw 'Unable to move to previous Page as that will be before the first page.';
		}
	};

	#changePageSize(size) {
		this.#currentPageSize = size;
		return this.#reload();
	};

	constructor(constructModel, url, isPaged, useGet, setParameters, currentParams, currentIndex, currentPageSize, pageVariableNames) {
		this.#constructModel = constructModel;
		this.#events = new EventHandler(['model_loaded', 'model_destroyed', 'model_updated', 'loaded']);
		this.#url = url;
		this.#useGet = useGet;
		this.#isPaged = isPaged;
		this.#setParameters = setParameters;
		this.#params = currentParams;
		this.#data = vue.reactive([]);
		if (isPaged) {
			this.#currentIndex = (currentIndex === undefined ? 0 : currentIndex);
			this.#currentPageSize = (currentPageSize === undefined ? 10 : currentPageSize);
			this.#pageVariableNames = pageVariableNames;
		}
		return this.#toProxy();
	};

	#toProxy() {
		let me = this;
		return new Proxy(this, {
			get(target, prop, reciever) {
				let ret = undefined;
				switch (prop) {
					case 'concat':
					case 'copyWithin':
					case 'fill':
					case 'pop':
					case 'push':
					case 'reverse':
					case 'shift':
					case 'sort':
					case 'splice':
					case 'unshift':
						throw 'Arrray is readonly';
						break;
					case 'length':
						ret = me.#data.length;
						break;
					case 'totalPages':
						ret = (me.#isPaged ? me.#totalPages : undefined);
						break;
					case 'currentPageSize':
						ret = (me.#isPaged ? me.#currentPageSize : undefined);
						break;
					case 'currentPage':
						ret = (me.#isPaged ? me.#currentPage : undefined);
						break;
					case 'moveToPage':
						ret = function (number) { return me.#moveToPage(number); };
						break;
					case 'moveToNextPage':
						ret = function () { return me.#moveToNextPage(); };
						break;
					case 'moveToPreviousPage':
						ret = function () { return me.#moveToPreviousPage(); };
						break;
					case 'changePageSize':
						ret = function (size) { return me.#changePageSize(size); };
						break;
					case '__proto__':
						ret = Array.__proto__;
						break;
					case 'reload':
						ret = function () { return me.#reload(); };
						break;
					case 'currentParameters':
						ret = new Proxy(me.#params, {
							get(target, prop, reciever) { return target[prop]; },
							set(target, prop, value) { throw 'The values are readonly'; }
						});
						break;
					case 'changeParameters':
						ret = function () {
							me.#setParameters.apply(me.#params, arguments);
							return me.#reload();
						};
						break;
					case 'toVueComposition':
						ret = function () {
							return me.#toVueComposition();
						};
						break;
					case '$on': ret = function (event, callback) { me.#events.on(event, callback); }; break;
					case '$off': ret = function (callback) { me.#events.off(callback); }; break;
					default:
						if (!isNaN(prop))
							ret = me.#data[prop];
						else if (me.#data[prop] != undefined)
							ret = function () { return me.#data[prop].apply(me.#data, arguments); };
						break;
				}
				return ret;
			},
			set(target, prop, value) {
				throw 'Arrray is readonly';
			},
			ownKeys(target) { return ['length', '$on', '$off', 'reload', 'toVueComposition'].concat((me.#isPaged ? ['totalPages', 'currentPageSize', 'currentPage', 'moveToPage', 'moveToNextPage', 'moveToPreviousPage', 'changePageSize'] : []).concat((me.#setParameters !== undefined ? ['currentParameters', 'changeParameters'] : []))); }
		});
	};

	#reload() {
		let tmp = this;
		let data = {};
		if (tmp.#isPaged) {
			data[tmp.#pageVariableNames["PageStartIndex"]] = tmp.#currentIndex;
			data[tmp.#pageVariableNames["PageSize"]] = tmp.#currentPageSize;
		}
		for (let prop in tmp.#params) {
			data[prop] = tmp.#params[prop];
		}
		return new Promise((resolve, reject) => {
			ajax({
				url: tmp.#url,
				method: (tmp.#useGet ? 'GET' : 'LIST'),
				credentials: 'include',
				useJSON:true,
				data: data
			}).then(
				response => {
					if (response.ok) {
						let data = response.json();
						if (data === null) {
							tmp.#totalPages = 0;
							Array.prototype.splice.apply(tmp.#data, [0, tmp.#data.length]);
						} else {
							if (data.TotalPages !== undefined) {
								tmp.#totalPages = data.TotalPages;
								data = data.response;
							}
							for (let i in data) {
								let mtmp = tmp.#constructModel();
								mtmp._parse(data[i]);
								data[i] = mtmp;
								data[i].$on('destroyed', function (model) {
									for (let x = 0; x < tmp.#data.length; x++) {
										if (tmp.#data[x].id === model.id) {
											tmp.#events.trigger('model_destroyed', model);
											Array.prototype.splice.apply(tmp.#data, [x, 1]);
											break;
										}
									}
								});
								data[i].$on('updated', function (model) {
									for (let x = 0; x < tmp.#data.length; x++) {
										if (tmp.#data[x].id === model.id) {
											tmp.#events.trigger('model_updated', model);
											Array.prototype.splice.apply(tmp.#data, [x, 0, model]);
											Array.prototype.splice.apply(tmp.#data, [x + 1, 1]);
											break;
										}
									}
								});
								data[i].$on('loaded', function (model) {
									for (let x = 0; x < tmp.#data.length; x++) {
										if (tmp.#data[x].id === model.id) {
											tmp.#events.trigger('model_loaded', model);
											Array.prototype.splice.apply(tmp.#data, [x, 0, model]);
											Array.prototype.splice.apply(tmp.#data, [x + 1, 1]);
											break;
										}
									}
								});
								Array.prototype.push.apply(tmp.#data, data);
								if (tmp.#data.length - data.length > 0) {
									{
										Array.prototype.splice.apply(tmp.#data, [0, tmp.#data.length - data.length]);
									}
								}
							}
						}
						let proxy = tmp.#toProxy();
						tmp.#events.trigger('loaded', proxy);
						resolve(proxy);
					} else {
						reject(response.text());
					}
				},
				rejected => {
					reject(rejected);
				}
			);
		});
	}
	#toVueComposition() {
		let me = this;
		let ret = {
			Items: vue.readonly(me.#data),
			reload: function () { return me.#reload(); },
			getEditableItem: function (index) { return me.#data[index]; }
		};
		if (this.#isPaged) {
			Object.assign(ret, {
				currentIndex: vue.readonly(vue.ref(me.#currentIndex)),
				currentPage: vue.readonly(vue.ref(me.#currentPage)),
				currentPageSize: vue.readonly(vue.ref(me.#currentPageSize)),
				totalPages: vue.readonly(vue.ref(me.#totalPages)),
				moveToPage: function (pageNumber) { return me.#moveToPage(pageNumber); },
				moveToNextPage: function () { return me.#moveToNextPage(); },
				moveToPreviousPage: function () { return me.#moveToPreviousPage(); },
				changePageSize: function (size) { return me.#changePageSize(size); }
			});
		}
		if (this.#setParameters !== undefined) {
			Object.assign(ret, {
				currentParameters: vue.readonly(me.#params),
				changeParameters: function () {
					me.#setParameters.apply(me.#params, arguments);
					return me.#reload();
				}
			});
		}
		return ret;
	};
};

const ModelMethods = {
	reload: function (url, id, isNew) {
		return new Promise((resolve, reject) => {
			if (isNew) {
				reject('Cannot reload unsaved model.');
			} else {
				ajax({
					url: url + '/' + id,
					method: 'GET'
				}).then(
					response => {
						if (response.ok) {
							let data = response.json();
							if (data == null) {
								reject(null);
							} else {
								resolve(data);
							}
						} else {
							reject(response.text());
						}
					},
					response => { reject(response.text()); }
				);
			}
		});
	},
	destroy: function (url, id, isNew) {
		return new Promise((resolve, reject) => {
			if (isNew) {
				reject('Cannot delete unsaved model.');
			} else {
				ajax(
					{
						url: url + '/' + id,
						method: 'DELETE'
					}).then(
						response => {
							if (response.ok) {
								let data = response.json();
								if (data) {
									resolve();
								} else {
									reject();
								}
							} else {
								reject(response.text());
							}
						},
						response => { reject(response.text()); }
					);
			}
		});
	},
	update: function (url, id, isNew, isValid, data, useJSON) {
		return new Promise((resolve, reject) => {
			if (!isValid) {
				reject('Invalid model.');
			} else if (isNew) {
				reject('Cannot update unsaved model, please call save instead.');
			} else {
				if (JSON.stringify(data) === JSON.stringify({})) {
					resolve(data);
				} else {
					ajax(
						{
							url: url + '/' + id,
							method: 'PATCH',
							useJSON: useJSON,
							data: data
						}).then(response => {
							if (response.ok) {
								let data = response.json();
								if (data) {
									resolve();
								} else {
									reject();
								}
							} else {
								reject(response.text());
							}
						}, response => { reject(response.text()); });
				}
			}
		});
	},
	save: function (url, isNew, isValid, data, useJSON) {
		return new Promise((resolve, reject) => {
			if (!isValid) {
				reject('Invalid model.');
			} else if (!isNew) {
				reject('Cannot save a saved model, please call update instead.');
			} else {
				ajax(
					{
						url: url,
						method: 'PUT',
						useJSON: useJSON,
						data: data
					}).then(response => {
						if (response.ok) {
							resolve(response.json());
						} else {
							reject(response.text());
						}
					},
						response => { reject(response.text()); });
			}
		});
	}
};


const vueFileReg = new RegExp('^.+\.vue$');
const _vueFileCache = new Map();

const _formatURL = function(url)
{
	let result = '';
	let upper = true;
	for (var i = 0; i < url.length; i++) {
		switch (url[i]) {
			case '.':
			case '-':
			case '_':
				upper = true;
				break;
			default:
				if (upper) {
					result += url[i].toUpperCase();
					upper = false;
				}
				else
					result += url[i];
				break;
		}
	}
	return result;
}

const cacheVueFile = function (url, content) {
	_vueFileCache.set(url.toLowerCase(), content);
	_vueFileCache.set(_formatURL(url).toLowerCase(), content);
}

const _fetchVueFile = async function (url) {
	if (vueFileReg.test(url)) {
		if (_vueFileCache.has(url.toLowerCase())) {
			return {
				getContentData: (asBinary) => _vueFileCache.get(url.toLowerCase())
			};
		} else {
			let nurl = _formatURL(url).toLowerCase();
			if (_vueFileCache.has(nurl)) {
				return {
					getContentData: (asBinary) => _vueFileCache.get(nurl)
				};
			} else {
				if (url.indexOf("http:") === 0 || url.indexOf("https:") === 0) {
					const res = await ajax({
						url: url,
						useJSON: false
					});
					if (!res.ok)
						throw Object.assign(new Error(res.text + ' ' + url), { res });
					cacheVueFile(url, res.text());
				} else {
					await import(url.substring(0, url.length - 4) + ".js");
				}
				return {
					getContentData: (asBinary) => _vueFileCache.get(url.toLowerCase())
				};
			}
		}
	} else {
		const res = await fetch(url);
		if (!res.ok) 
			throw Object.assign(new Error(res.statusText + ' ' + url), { res });
		return {
			getContentData: (asBinary) => asBinary ? res.arrayBuffer() : res.text()
		};
	}
}

const _cachedCode = [];

const _moduleCache = { vue: vue } ;

const vueSFCOptions = {
	moduleCache: _moduleCache,
	compiledCache: {
		set(key, str) {
			_cachedCode.push(key);
			var success = false;
			while (!success && _cachedCode.length>0) {
				try {
					window.sessionStorage.setItem(key, str);
					success = true;
				} catch (ex) {
					window.sessionStorage.removeItem(_cachedCode.shift());
				}
			}
		},
		get(key) {
			if (_cachedCode.indexOf(key) >= 0)
				return window.sessionStorage.getItem(key);
			return null;
		}
	},
	async getFile(url) {
		return _fetchVueFile(url);
	},
	addStyle(textContent) {
		const style = Object.assign(document.createElement('style'), { textContent });
		const ref = document.head.getElementsByTagName('style')[0] || null;
		document.head.insertBefore(style, ref);
	}
}

export { isString, isFunction, cloneData, ajax, isEqual, checkProperty, stripBigInt, EventHandler, ModelList, ModelMethods, cacheVueFile, vueSFCOptions};