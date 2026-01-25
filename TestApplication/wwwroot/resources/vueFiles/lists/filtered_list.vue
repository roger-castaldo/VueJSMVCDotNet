<template>
    <table>
        <thead>
            <tr>
                <th colspan="100%">Filtered Person List</th>
            </tr>
            <tr>
                <th colspan="100%">
                    <input type="text" v-model="current_filter" />
                </th>
            </tr>
            <tr>
                <th>First Name</th>
                <th>Last Name</th>
                <th>FullName</th>
            </tr>
        </thead>
        <tbody v-if="Items!=null">
            <tr v-for="person in Items">
                <td>{{person.FirstName}}</td>
                <td>{{person.LastName}}</td>
                <asynccomp v-bind:promise="person.GetFullName()">
                    <template #resolved="props">
                        {{props}}
                    </template>
                    <template #rejected="props">
                        <span style="color:red">{{props}}</span>
                    </template>
                </asynccomp>
            </tr>
        </tbody>
    </table>
</template>

<script setup>
    import { mPerson } from '../../../models/mPerson.js';
    import { watch, ref } from "vue";
    import asynccomp from "../asynccomp.vue";

    const { Items, changeParameters, $on } = mPerson.Search(null).toVueComposition();
    $on('loaded', () => console.log('Filtered Items loaded'));
    let current_filter = ref('');

    console.log(Items);

    watch(
        current_filter,
        (newValue, oldValue) => {
            changeParameters((newValue === '' ? null : newValue));
        }
    );

    defineExpose({
        Items,
        current_filter
    });
</script>