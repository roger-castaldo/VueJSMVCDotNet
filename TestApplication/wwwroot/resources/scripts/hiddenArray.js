import { reactive, readonly } from 'vue';

export default function (props, { attrs, slots, emit, expose }) {
    const data = reactive([]);

    const addItem = (item) => { data.push(item) };

    return { Items: readonly(data), append: addItem };
}