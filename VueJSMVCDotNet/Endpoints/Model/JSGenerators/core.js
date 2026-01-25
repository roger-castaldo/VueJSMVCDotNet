const isString = (value) => typeof value === 'string';

const isFunction = (obj) => obj !== null && typeof obj === 'function';

const _isDate = (obj) => Object.prototype.toString.call(obj) === '[object Date]';

const _isObject = (obj) => obj !== null && obj !== undefined && !(obj.toString() === '[object FileList]' || obj.toString() === '[object File]')
	&& !_isDate(obj) && (['function', 'object'].includes(typeof obj) >= 0 && !!obj);

const cloneData = (obj) => {
	if (obj === null) return null;
	if (!_isObject(obj) && !Array.isArray(obj)) return obj;
	if (Array.isArray(obj)) return obj.map(o => cloneData(o));
	let ret = {};
	Object.getOwnPropertyNames(obj)
		.filter(prop => !isFunction(obj[prop]))
		.forEach(prop => ret[prop] = cloneData(obj[prop]));
	return ret;
};

const _dateRegex = /^\d{4}-((0[1-9])|(1[0-2]))-((0[1-9])|([12]\d)|(3[01]))T([0-5]\d):([0-5]\d):([0-5]\d).\d{3}Z$/;

const _fixDates = (data) => {
	if (data === null) return data;
	else if (Array.isArray(data)) {
		data = data.map(val => {
			if (isString(val) && _dateRegex.test(val))
				return new Date(val);
			else if (Array.isArray(val) || _isObject(val))
				return _fixDates(val);
			return val;
		});
	}
	else if (_isObject(data)) Object.keys(data).forEach(prop => data[prop] = _fixDates(data[prop]));
	else if (isString(data) && _dateRegex.test(data)) data = new Date(data);
	return data;
};

const processSlowCall = async (options, ajax) => {
	delete options.isSlow;
	let isArray = (options.isArray == undefined ? false : options.isArray);
	let res = await ajax(options);
	let ret = [];
	let url = await res.json();
	return await new Promise((resolve, reject) => {
		const pullCall = async () => {
			let res = null;
			try {
				res = await ajax({
					url: url,
					method: 'GET',
					useJSON: true
				});
			} catch (err) {
				ret = null;
				reject(err);
				return;
			}
			if (!res.ok) {
				reject(res.text());
				return;
			}
			res = res.json();
			if (res.Data.length > 0)
				Array.prototype.push.apply(ret, res.Data);
			if (res.IsFinished) {
				if (!isArray) {
					ret = (ret.length == 0 ? null : ret[0]);
				}
				resolve({
					json() { return ret; }
				});
			} else {
				setTimeout(pullCall, (res.HasMore ? 0 : 200));
			}
		};
		pullCall();
	});
};

const processOptionsBody = (options) => {
	if (options.method === 'GET') {
		delete options.body;
	} else {
		let data = null;
		if (options.data !== null) {
			if (options.useJSON) {
				data = JSON.stringify(stripBigInt(options.data));
			} else {
				data = new FormData();
				Object.keys(options.data).forEach(prop => {
					if (Array.isArray(options.data[prop])) {
						if (options.data[prop].length > 0) {
							if (_isObject(options.data[prop][0]))
								options.data[prop].forEach(val => data.append(`${prop}:json`, JSON.stringify(val)));
							else
								options.data[prop].forEach(val => data.append(prop, val));
						}
					} else if (_isObject(options.data[prop]))
						data.append(`${prop}:json`, JSON.stringify(options.data[prop]));
					else
						data.append(prop, options.data[prop]);
				});
			}
		}
		options.body = data;
	}
	return options;
};

const ajax = async (options) => {
	if (options.url === null || options.url === undefined || options.url === '') {
		throw new Error('Unable to call empty url');
	}
	if (options.isSlow !== undefined && options.isSlow) {
		processSlowCall(options, ajax);
	} else {
		options = {
			method: 'GET',
			body: null,
			credentials: 'include',
			data: null,
			url: null,
			useJSON: true,
			headers: {},
			... options
		};
		if (options.useJSON) {
			options.headers['Content-Type'] = 'application/json';
		}
		options.url += (options.url.includes('?') === -1 ? '?' : '&') + '_=' + Number.parseInt((Date.now() / 1000).toFixed(0)).toString();
		options = processOptionsBody(options);
		let url = options.url;
		delete options.url;
		try {
			let response = await fetch(url, options);
			let content = await response.text();
			if (response.ok) {
				return {
					ok: true,
					text() { return content; },
					json() { return (response.headers.get('Content-Type') === 'text/text' ? content : _fixDates(JSON.parse(content))); }
				};
			} else {
				return {
					ok: false,
					text() { return content; }
				};
			}
		} catch (err) {
			return {
				ok: false,
				text() { return err; }
			};
		}
	}
};

const _areArraysEqual = (isEqual, a, b) => {
	if (!Array.isArray(b) || a.length !== b.length) return false;

	for (let i = 0; i < a.length; i++) {
		if (!isEqual(a[i], b[i], visited)) return false;
	}
	return true;
}

const _areObjectsEqual = (isEqual, a, b, visited) => {
	// Object comparison
	const keys = new Set([
		...Object.keys(a),
		...Object.keys(b)
	]);

	for (const key of keys) {
		const valA = a[key];
		const valB = b[key];

		// Treat missing and undefined as equal
		const hasA = Object.hasOwn(a, key);
		const hasB = Object.hasOwn(b, key);

		if (!hasA && valB === undefined) continue;
		if (!hasB && valA === undefined) continue;

		if (!isEqual(valA, valB, visited)) return false;
	}
    return true;
}

const isEqual = (a, b, visited = new WeakMap()) => {
	if (a === b) return true;
	if (Number.isNaN(a) && Number.isNaN(b)) return true;
	if (a === null || b === null || typeof a !== 'object' || typeof b !== 'object') {
		return false;
	}

	if (typeof a !== typeof b) {
		return false;
	}

	// Circular reference handling
	if (visited.has(a)) {
		return visited.get(a) === b;
	}
	visited.set(a, b);

	// Date comparison
	if (a instanceof Date) {
		return a.getTime() === b.getTime();
	} else if (Array.isArray(a)) {
        return _areArraysEqual(isEqual, a, b);
	}
	return _areObjectsEqual(isEqual, a, b, visited);
};

const _numberRanges = {
	'Int16': { low: -32768, high: 32767, hasDecimal: false },
	'Int32': { low: -2147483648, high: 2147483647, hasDecimal: false },
	'Int64': { low: Number('-9223372036854775808'), high: Number('9223372036854775807'), hasDecimal: false },
	'SByte': { low: -128, high: 127, hasDecimal: false },
	'Single': { low: Number('-3.402823e38'), high: Number('3.402823e38'), hasDecimal: true },
	'Decimal': { low: Number('-79228162514264337593543950335'), high: Number('79228162514264337593543950335'), hasDecimal: true },
	'Double': { low: Number('-1.7976931348623157E+308'), high: Number('1.7976931348623157E+308'), hasDecimal: true },
	'UInt16': { low: 0, high: 65535, hasDecimal: false },
	'UInt32': { low: 0, high: 4294967295, hasDecimal: false },
	'UInt64': { low: Number(0), high: Number('18446744073709551615'), hasDecimal: false },
	'Byte': { low: 0, high: 255, hasDecimal: false },
};

const _trueRegex = /^(t(rue)?|y(es)?|1)$/i;
const _falseRegex = /^(f(alse)?|n(o)?|0)$/i;
const _base64Regex = /^([0-9a-zA-Z+/]{4})*(([0-9a-zA-Z+/]{2}==)|([0-9a-zA-Z+/]{3}=))?$/;
const _versionRegex = /^(\d+)\.(\d+)(\.(\d+))?(\.(\d+))?$/;
const _ipv4Regex = /^(?!0)(?!.*\.$)((1?\d?\d|25[0-5]|2[0-4]\d)(\.|$)){4}$/;
const h = '[0-9a-f]';
const _ipv6Regex = new RegExp(
	`^((?:${h}{1,4}:){7}${h}{1,4}|` +
	`(?:${h}{1,4}:){1,7}:|` +
	`(?:${h}{1,4}:){1,6}:${h}{1,4}|` +
	`(?:${h}{1,4}:){1,5}(?::${h}{1,4}){1,2}|` +
	`(?:${h}{1,4}:){1,4}(?::${h}{1,4}){1,3}|` +
	`(?:${h}{1,4}:){1,3}(?::${h}{1,4}){1,4}|` +
	`(?:${h}{1,4}:){1,2}(?::${h}{1,4}){1,5}|` +
	`${h}{1,4}:(?::${h}{1,4}){1,6}|` +
	`:(?::${h}{1,4}){1,7}|::)$`,
	'i'
);
const _guidRegex = new RegExp(
	`^(${h}{8})-(${h}{4})-([1-5]${h}{3})-([89ab]${h}{3})-(${h}{12})$`,
	'i'
);

const _numberValueInRange = (value, low, high) => {
	if (typeof value === 'bigint' && typeof low !== 'bigint') {
		low = BigInt(low);
		high = BigInt(high);
	}
	if (typeof value !== 'bigint' && typeof low === 'bigint') {
		value = BigInt(value);
	}
	return value >= low && value <= high;
};

const _checkNumber = (type, value) => {
	if (typeof value !== 'number' && typeof value !== 'bigint') {
		if (typeof value !== 'string' && value.toString === undefined)
			value = value.toString();
		if (typeof value === 'string') {
			try {
				value = Number(value);
			} catch{
			}
		}
	}
	if (typeof value !== 'bigint' && Number.isNaN(value))
		throw new Error('invalid type: Value not a number and cannot be converted');
	if (!_numberValueInRange(value, _numberRanges[type].low, _numberRanges[type].high))
		throw new Error(`invalid type: Value is a number, but exceeds the range for a ${type}`);
	if (!_numberRanges[type].hasDecimal && value.toString().includes('.'))
		throw new Error(`invalid type: Value is a number, but cannot has a decimal for ${type}`);
	return value;
}

const _checkIP = (value) => {
	if (typeof value !== 'string' && value.toString === undefined)
		value = value.toString();
	if (!_ipv4Regex.test(value)) {
		if (!_ipv6Regex.test(value)) {
			throw new Error('invalid type: Value is not an IPAddress');
		}
	}
	return value;
};

const dataCheckCallbacks = {
	'String': (value) => {
		if (typeof value !== 'string' && value.toString === undefined)
			throw new Error('invalid type: Value not a string and cannot be converted');
		else if (typeof value !== 'string' && value.toString !== undefined) {
			if (value.toString() === '[object Object]')
				throw new Error('invalid type: Value not a string and cannot be converted');
			value = value.toString();
		}
		return value;
	},
	'Char': (value) => {
		if (typeof value !== 'string' && value.toString === undefined)
			throw new Error('invalid type: Value not a char and cannot be converted');
		else if (typeof value !== 'string' && value.toString !== undefined)
			value = value.toString();
		if (value.length > 1 || value.length == 0)
			throw new Error('invalid type: Value not a char');
		return value;
	},
	'UInt64': (value) => _checkNumber('UInt64', value),
	'Int64': (value) => _checkNumber('Int64', value),
	'Int16': (value) => _checkNumber('Int16', value),
	'Int32': (value) => _checkNumber('Int32', value),
	'SByte': (value) => _checkNumber('SByte', value),
	'Single': (value) => _checkNumber('Single', value),
	'Decimal': (value) => _checkNumber('Decimal', value),
	'Double': (value) => _checkNumber('Double', value),
	'UInt16': (value) => _checkNumber('UInt16', value),
	'UInt32': (value) => _checkNumber('UInt32', value),
	'Byte': (value) => _checkNumber('Byte', value),
	'Boolean': (value) => {
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
				throw new Error('invalid type: Value not boolean and cannot be converted');
		}
		return value;
	},
	'Enum': (value, enumlist) => {
		if (typeof value !== 'string' && value.toString === undefined)
			value = value.toString();
		if (enumlist !== undefined && enumlist !== null) {
			if (!enumlist.includes(value))
				throw new Error('invalid type: Value is not in the list of enumarators');
		}
		return value;
	},
	'DateTime': (value) => {
		if (Object.prototype.toString.call(value) !== '[object Date]') {
			try {
				value = new Date(value);
				if (value.toString() ==='Invalid Date')
					throw new Error('invalid type: Value is not a Date and cannot be converted to one');
			} catch {
				throw new Error('invalid type: Value is not a Date and cannot be converted to one');
			}
		}
		return value;
	},
	'Byte[]': (value) => {
		if (value.byteLenth === undefined) {
			if (typeof value !== 'string' && value.toString === undefined)
				value = value.toString();
			if (!_base64Regex.test(value))
				throw new Error('invalid type: Value is not a Byte[] and cannot be converted to one');
			value = atob(value); //invert this is btoa
		}
		return value;
	},
	'Net.IPAddress': (value) => _checkIP(value),
	'IPAddress': (value) => _checkIP(value),
	'Version': (value) => {
		if (typeof value !== 'string' && value.toString === undefined)
			value = value.toString();
		if (!_versionRegex.test(value))
			throw new Error('invalid type: Value is not a Version');
		return value;
	},
	'Guid': (value) => {
		if (typeof value !== 'string' && value.toString === undefined)
			value = value.toString();
		if (!_guidRegex.test(value))
			throw new Error('invalid type: Value is not a Guid');
		return value;
	},
	'Exception': (value) => {
		if (typeof value === 'string') {
			try {
				value = JSON.parse(value);
			} catch { }
		}
		if (Object.prototype.toString.call(value) !== '[object String]') {
			if (typeof value === 'object' && !Array.isArray(value)) {
				try {
					_checkDataType('String', value.Message);
					_checkDataType('String', value.StackTrace);
					_checkDataType('String', value.Source);
				} catch {
					throw new Error('invalid type: Value is not an Exception');
				}
			} else
				throw new Error('invalid type: Value is not an Exception');
		}
		return value;
	}
};

const _checkDataType = (type, value, enumlist) => {
	if (type.indexOf('System.') === 0)
		type = type.substring(7);
	if (type.substring(type.length - 1) === '?') {
		if (value === null || value === undefined)
			return value;
		type = type.substring(0, type.length - 1);
	} else {
		if (type !== 'Boolean' && (value === null || value === undefined))
			throw new Error('invalid type: Value is not allowed to be null');
	}
	if (type === 'IFormFile[]' && value.toString() !== '[object FileList]') 
		throw new Error('invalid type: Value not a FileList and cannot be converted');
	else if (type === 'IFormFile' && value.toString() !== '[object File]') 
		throw new Error('invalid type: Value not a File and cannot be converted');
	else if (type.includes('[]') && type !== 'Byte[]') {
		if (!Array.isArray(value))
			throw new Error('invalid type: Value not an array');
		type = type.substring(0, type.length - 2);
		if (!type.includes('[]')) {
			type = type + '?';
		}
		for (let x = 0; x < value.length; x++) {
			try {
				value[x] = _checkDataType(type, value[x]);
			} catch {
				throw new Error(`invalid type: Value[${x}] is not of the type ${type}`);
			}
		}
	} else {
		value = dataCheckCallbacks[type](value, enumlist);
	}
	return value;
};

const checkProperty = (name, type, value, enumlist) => {
	try {
		return _checkDataType(type, value, enumlist);
	} catch (err) {
		throw new Error(`Cannot set ${name}: ${err.message??err}`);
	}
};

const stripBigInt = (data) => {
	if (data === null || data === undefined || Object.prototype.toString.call(data) === '[object Date]') return data;
	if (Array.isArray(data)) return data.map(value => stripBigInt(value));
	else if (typeof data === 'bigint') return data.toString();
	else if (typeof data === 'object') {
		let result = {};
		Object.keys(data).filter(prop => prop !== '_hashCode').forEach(prop => result[prop] = stripBigInt(data[prop]));
		return result;
	}
	return data;
};

class EventHandler {
	#events;
	constructor(events) {
		this.#events = {};
		events.forEach(prop => this.#events[prop] = []);
	}

	on(event, callback) {
		if (this.#events[event] === undefined) { throw new Error('undefined event'); }
		this.#events[event].push(callback);
	}

	off(callback) {
		Object.keys(this.#events)
			.forEach(prop => {
				this.#events[prop] = this.#events[prop].filter(c => c === callback);
			});
	}

	trigger(event, data) {
		if (this.#events[event] === undefined) { throw new Error('undefined event'); }
		this.#events[event].forEach(call => call(data));
	}
};

class ModelList {
	#events;
	#data;
	#isPaged;
	#constructModel;
	#url;
	#useGet;
	#params = undefined;
	#setParameters = undefined;
	#totalPages = vue.ref(null);
	#currentIndex = vue.ref(null);
	#currentPageSize = vue.ref(null);
	#pageVariableNames = undefined;
	#currentPage = vue.computed(() => (this.#isPaged ? Math.floor(this.#currentIndex.value / this.#currentPageSize.value) : undefined ));


	#moveToPage(pageNumber) {
		if (pageNumber >= this.#totalPages.value || pageNumber < 0) {
			throw new Error('Unable to move to Page that is outside the page range.');
		} else {
			this.#currentIndex.value = pageNumber * this.#currentPageSize.value;
			return this.#reload();
		}
	};

	#moveToNextPage() {
		if ((this.#currentPage.value + 1) < this.#totalPages.value) {
			return this.#moveToPage(this.#currentPage.value + 1);
		} else {
			throw new Error('Unable to move to next Page as that will excess current total pages.');
		}
	};

	#moveToPreviousPage() {
		if ((this.#currentPage.value) > 0) {
			return this.moveToPage(this.#currentPage.value - 1);
		} else {
			throw new Error('Unable to move to previous Page as that will be before the first page.');
		}
	};

	#changePageSize(size) {
		this.#currentPageSize.value = size;
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
			this.#currentIndex.value = currentIndex ?? 0;
			this.#currentPageSize.value = currentPageSize ?? 10;
			this.#pageVariableNames = pageVariableNames;
		}
		this.#reload();
		return this.#toProxy();
	};

	#toProxy() {
		let me = this;
		return new Proxy(this, {
			get(target, prop, reciever) {
				let ret;
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
						throw new Error('Array is readonly');
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
					case 'reload':
						ret = function () { return me.#reload(); };
						break;
					case 'currentParameters':
						ret = new Proxy(me.#params, {
							get(target, prop, reciever) { return target[prop]; },
							set(target, prop, value) { throw new Error('The values are readonly'); }
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
						if (!Number.isNaN(prop))
							ret = me.#data[prop];
						else if (me.#data[prop] != undefined)
							ret = function () { return me.#data[prop].apply(me.#data, arguments); };
						break;
				}
				return ret;
			},
			set(target, prop, value) {
				throw new Error('Arrray is readonly');
			},
			ownKeys(target) { return ['length', '$on', '$off', 'reload', 'toVueComposition'].concat((me.#isPaged ? ['totalPages', 'currentPageSize', 'currentPage', 'moveToPage', 'moveToNextPage', 'moveToPreviousPage', 'changePageSize'] : []).concat((me.#setParameters === undefined ? [] : ['currentParameters', 'changeParameters']))); }
		});
	};

	async #reload() {
		let tmp = this;
		let data = {};
		if (tmp.#isPaged) {
			data[tmp.#pageVariableNames["PageStartIndex"]] = tmp.#currentIndex.value;
			data[tmp.#pageVariableNames["PageSize"]] = tmp.#currentPageSize.value;
		}

		if (tmp.#params !== undefined) {
			for (let prop in tmp.#params) {
				data[prop] = tmp.#params[prop];
			}
		}
		let response = await ajax({
			url: tmp.#url,
			method: (tmp.#useGet ? 'GET' : 'POST'),
			credentials: 'include',
			useJSON: true,
			data: data
		});
		if (response.ok) {
			let data = response.json();
			if (data === null) {
				tmp.#totalPages.value = 0;
				tmp.#data.splice(0, tmp.#data.length);
			} else {
				if (data.totalPages !== undefined) {
					tmp.#totalPages.value = data.totalPages;
				}
				data = (data.totalPages === undefined ? data : data.data).map(value => {
					let mtmp = tmp.#constructModel();
					mtmp._parse(value);
					mtmp.$on('destroyed', (model) => {
						let idx = tmp.#data.findIndex((element) => element.id === modelid);
						tmp.#events.trigger('model_destroyed', model);
						tmp.#data.splice(idx, 1);
					});
					mtmp.$on('updated', (model) => {
						let idx = tmp.#data.findIndex((element) => element.id === modelid);
						tmp.#events.trigger('model_updated', model);
						tmp.#data.splice(idx, 1, model);
					});
					mtmp.$on('loaded', (model) => {
						let idx = tmp.#data.findIndex((element) => element.id === modelid);
						tmp.#events.trigger('model_loaded', model);
						tmp.#data.splice(idx, 1, model);
					});
					return mtmp;
				});
				tmp.#data.push(...data);
				if (tmp.#data.length - data.length > 0) tmp.#data.splice(0, tmp.#data.length - data.length);
			}
			let proxy = tmp.#toProxy();
			tmp.#events.trigger('loaded', proxy);
			return proxy;
		} else {
			throw new Error(response.text());
		}
	};

	#toVueComposition() {
		let me = this;
		let ret = {
			Items: vue.readonly(me.#data),
			reload: function () { return me.#reload(); },
			getEditableItem: function (index) { return me.#data[index]; },
			$on: function (event, callback) { me.#events.on(event, callback); },
			$off: function (callback) { me.#events.off(callback); }
		};
		if (this.#isPaged) {
			Object.assign(ret, {
				currentIndex: vue.readonly(me.#currentIndex),
				currentPage: me.#currentPage,
				currentPageSize: vue.readonly(me.#currentPageSize),
				totalPages: vue.readonly(me.#totalPages),
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
	reload: async function (url, id, isNew) {
		if (isNew) {
			throw new Error('Cannot reload unsaved model.');
		} else {
			let response = await ajax({
				url: url + '/' + id,
				method: 'GET'
			});
			if (response.ok) {
				let data = response.json();
				if (data == null) {
					throw new Error('reload failed');
				} else {
					return data;
				}
			} else {
				throw new Error(response.text());
			}
		}
	},
	destroy: async function (url, id, isNew) {
		if (isNew) {
			throw new Error('Cannot delete unsaved model.');
		} else {
			let response = await ajax({
				url: url + '/' + id,
				method: 'DELETE'
			});
			if (response.ok) {
				let data = response.json();
				if (data == null) {
					throw new Error('delete failed');
				} else {
					return data;
				}
			} else {
				throw new Error(response.text());
			}
		}
	},
	update: async function (url, id, isNew, isValid, data, useJSON) {
		if (!isValid) {
			throw new Error('Invalid model.');
		} else if (isNew) {
			throw new Error('Cannot update unsaved model, please call save instead.');
		} else if (JSON.stringify(data) === JSON.stringify({})) {
			return data;
		} else {
			let response = await ajax({
				url: url + '/' + id,
				method: 'PATCH',
				useJSON: useJSON,
				data: data
			});
			if (response.ok) {
				let data = response.json();
				if (data) {
					return {};
				} else {
					throw new Error('update failed');
				}
			} else {
				throw new Error(response.text());
			}
		}
	},
	save: async function (url, isNew, isValid, data, useJSON) {
		if (!isValid) {
			throw new Error('Invalid model.');
		} else if (!isNew) {
			throw new Error('Cannot save a saved model, please call update instead.');
		} else {
			let response = await ajax({
				url: url,
				method: 'PUT',
				useJSON: useJSON,
				data: data
			});
			if (response.ok) {
				let result = response.json();
				if (result === null)
					throw new Error('save failed');
				return result;
			} else {
				throw new Error(response.text());
			}
		}
	}
};

//Messages section

const _language = vue.ref(null);

const ResetLanguage = function () {

	let lang = 'en';
	if (globalThis !== undefined && globalThis.navigator !== undefined)
		lang = globalThis.navigator.userLanguage || globalThis.navigator.language;
	if (lang.includes('-')) {
		lang = lang.substring(0, lang.indexOf('-'));
	}
	_language.value = lang;
}

ResetLanguage();

const Language = vue.readonly(_language);

const SetLanguage = function (language) {
	_language.value = language;
}

export { isString, isFunction, cloneData, ajax, isEqual, checkProperty, stripBigInt, EventHandler, ModelList, ModelMethods, Language, SetLanguage, ResetLanguage };