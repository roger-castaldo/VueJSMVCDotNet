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
                        {{props.result}}
                    </template>
                    <template #rejected="props">
                        <span style="color:red">{{props.result}}</span>
                    </template>
                </asynccomp>
            </tr>
        </tbody>
    </table>
</template>

<script setup>
    import { mPerson } from "/testing/resources/scripts/mPerson.js";
    import { watch, ref, expose } from "vue";
    import asynccomp from "./asynccomp.js";

    const { Items, changeParameters } = mPerson.Search(null).toVueComposition();
    let current_filter = ref('');

    watch(
        current_filter,
        (newValue, oldValue) => {
            changeParameters((newValue === '' ? null : newValue));
        }
    );

    expose({
        Items,
        current_filter
    });
</script>