<template>
        <table>
            <thead>
                <tr>
                    <th colspan="100%">Full Person List</th>
                </tr>
                <tr>
                    <th>First Name</th>
                    <th>Last Name</th>
                    <th>FullName</th>
                    <th>
                        <button v-on:click="CreateNew">Create New</button>
                    </th>
                </tr>
            </thead>
            <tbody v-if="Items!=null">
                <tr v-for="person,index in Items">
                    <td>{{person.FirstName}}</td>
                    <td>{{person.LastName}}</td>
                    <td>
                        <asynccomp v-bind:promise="person.GetFullName()" resultname="fullName">
                            <template #resolved="props">
                                {{props.fullName}}
                            </template>
                            <template #rejected="result">
                                <span style="color:red">{{result}}</span>
                            </template>
                        </asynccomp>
                    </td>
                    <td>
                        <button v-on:click="person.destroy()">Delete</button>
                        <button v-on:click="EditPerson(index)">Edit</button>
                    </td>
                </tr>
            </tbody>
        </table>
</template>

<script setup>
    import { mPerson } from "/testing/resources/scripts/mPerson.js";
    import { watch,expose } from 'vue';

    console.log('loading list vue');

    const { Items, reload, getEditableItem } = mPerson.LoadAll().toVueComposition();
    expose({
        reload: reload
    });

    watch(
        () => Items,
        (newValue, oldValue) => {
            console.log('Items changed');
        },
        { deep: true }
    );

    function EditPerson(index) {
        frm.person = getEditableItem(index);
    }

    function CreateNew() {
        frm.lst = this;
        frm.person = mPerson.createInstance();
    }
</script>