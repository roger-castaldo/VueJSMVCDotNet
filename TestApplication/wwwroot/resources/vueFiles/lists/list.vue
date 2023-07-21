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
            <tr v-for="p,index in Items">
                <td>{{p.FirstName}}</td>
                <td>{{p.LastName}}</td>
                <td>
                    <asynccomp v-bind:promise="p.GetFullName()" resultname="fullName">
                        <template #resolved="props">
                            {{props.fullName}}
                        </template>
                        <template #rejected="result">
                            <span style="color:red">{{result}}</span>
                        </template>
                    </asynccomp>
                </td>
                <td>
                    <button v-on:click="p.destroy()">Delete</button>
                    <button v-on:click="EditPerson(index)">Edit</button>
                </td>
            </tr>
        </tbody>
    </table>
    <table v-show="person!=null">
        <thead>
            <tr>
                <th colspan="100%">
                    <template v-if="person!=null && person.isNew()">
                        Create
                    </template>
                    <template v-if="person!=null && !person.isNew()">
                        Edit
                    </template>
                </th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td>First Name:</td>
                <td><input type="text" v-model="FirstName" /></td>
            </tr>
            <tr>
                <td>Last Name:</td>
                <td><input type="text" v-model="LastName" /></td>
            </tr>
            <tr>
                <td>Test File:</td>
                <td><input type="file" @change="onFileChange($event)" /></td>
            </tr>
        </tbody>
        <tfoot>
            <tr>
                <td colspan="100%">
                    <button v-on:click="Save" :disabled="FirstName==null||LastName==null">
                        <template v-if="person!=null && person.isNew()">
                            Create
                        </template>
                        <template v-if="person!=null && !person.isNew()">
                            Update
                        </template>
                    </button>
                    <button v-on:click="person=null">
                        Cancel
                    </button>
                    <button v-on:click="UploadFile">
                        Upload File
                    </button>
                </td>
            </tr>
        </tfoot>
    </table>
</template>

<script setup>
    import { mPerson } from "/testing/resources/scripts/mPerson.js";
    import { watch, expose,ref } from 'vue';

    console.log('loading list vue');

    const person = ref(null);
    const FirstName = ref(null);
    const LastName = ref(null);
    const file = ref(null);

    watch(person, (val) => {
        if (val == null) {
            FirstName.value = null;
            LastName.value = null;
        } else {
            FirstName.value = val.FirstName;
            LastName.value = val.LastName;
            console.log(val.id);
        }
    });

    const { Items, reload, getEditableItem } = mPerson.LoadAll().toVueComposition();

    watch(
        () => Items,
        (newValue, oldValue) => {
            console.log('Items changed');
        },
        { deep: true }
    );

    function EditPerson(index) {
        person.value = getEditableItem(index);
    }

    function CreateNew() {
        person.value = mPerson.createInstance();
    }

    function onFileChange($event) {
        file.value = $event.target.files[0];
    };

    function Save() {
        person.FirstName = FirstName;
        person.LastName = LastName;
        if (person.isNew()) {
            person.save().then(success => {
                console.log(success);
                reload();
                person = null;
            });
        } else {
            person.update().then(success => {
                view.person = null;
            });
        }
    };

    function UploadFile() {
        mPerson.ReadFile(file.value)
            .then(result => { alert(result); });
    };
</script>