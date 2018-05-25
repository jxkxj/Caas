//Initialize
(function ($) {
    $(function () {

        //initialize all modals           
        $('.modal').modal();

        //intialize all selects
        $('select').material_select();
    }); // end of document ready
})(jQuery); // end of jQuery name space

var clientApp = new Vue({
    el: '#swipe-clients',
    data: {
        clients: []
    },
    methods: {
        loadData: function() {
            this.$http.get('/api/config/getallclients').then(response => {
                this.clients = response.body
                manageConfigAssociationsApp.clients = response.body
            })
        },
        editClient: function(client) {
            $("select").material_select()
            $("#clientTypeSelection").val(client.clientType.name)
            $("#clientTypeSelection").material_select(client.clientType.name)
            manageClientApp.mode = 'edit'
            manageClientApp.client = {
                ClientId: client.clientId,
                Identifier: client.identifier,
                ClientType: {
                    Name: client.clientType.name
                },
                Created: client.created,
                Updated: client.updated
            }
            $("#ManageClientModal").modal('open')
        },
        associateConfigs: function(client) {
            manageConfigAssociationsApp.direction = 'client'
            manageConfigAssociationsApp.client = client
            manageConfigAssociationsApp.loadAssociations()
            $("select").material_select()
            $("#ManageConfigAssociationsModal").modal('open')
        },
        deleteClient: function(clientId) {
            var r = confirm("Are you sure you want to delete this client?")
            if(r == true) {
                loadingApp.loading = true
                this.$http.delete('/api/config/deleteclient?clientId=' + clientId).then(response => {
                    this.loadData()  
                    loadingApp.loading = false
                })
            }
        }
    },
    created: function() {
        this.loadData()
    }
})

var clientTypeApp = new Vue({
    el: '#swipe-client-types',
    data: {
        clientTypes: []
    },
    methods: {
        loadData: function() {
            this.$http.get('/api/config/getallclienttypes').then(response => {
                this.clientTypes = response.body
                manageClientApp.clientTypes = response.body
            })
        },
        editClientType: function(clientType) {
            manageClientTypeApp.mode = 'edit'
            manageClientTypeApp.clientType = {
                ClientTypeId: clientType.clientTypeId,
                Name: clientType.name,
                Created: clientType.created,
                Updated: clientType.updated
            }
            $("#ManageClientTypeModal").modal('open')
        },
        deleteClientType: function(clientTypeId) {
            var r = confirm("Are you sure you want to delete this client type (it will also remove all clients with this type)?")
            if(r == true) {
                loadingApp.loading = true
                this.$http.delete('/api/config/deleteclienttype?clientTypeId=' + clientTypeId).then(response => {
                    this.loadData()  
                    loadingApp.loading = false
                })
            }
        }
    },
    created: function() {
        this.loadData()   
    }
})

var configApp = new Vue({
    el: '#swipe-configs',
    data: {
        configs: []
    },
    methods: {
        loadData: function() {
            this.$http.get('/api/config/getallconfigs').then(response => {
                this.configs = response.body
                manageConfigAssociationsApp.configs = response.body
            })
        },
        editConfig: function(config) {
            manageConfigApp.mode = 'edit'
            manageConfigApp.config = {
                ConfigId: config.configId,
                Key: config.key,
                Value: config.value,
                Created: config.created,
                Updated: config.updated
            }
            $("#ManageConfigModal").modal('open')
        },
        associateClients: function(config) {
            manageConfigAssociationsApp.direction = 'config'
            manageConfigAssociationsApp.config = config
            manageConfigAssociationsApp.loadAssociations()
            $("select").material_select()
            $("#ManageConfigAssociationsModal").modal('open')
        },
        deleteConfig: function(configId) {
            var r = confirm("Are you sure you want to delete this config?")
            if(r == true) {
                loadingApp.loading = true
                this.$http.delete('/api/config/deleteconfig?configId=' + configId).then(response => {
                    this.loadData()  
                    loadingApp.loading = false
                })
            }
        }
    },
    created: function() {
        this.loadData()
    }
})

var checkInApp = new Vue({
    el: '#swipe-check-ins',
    data: {
        checkIns: []
    },
    methods: {
        loadData: function() {
            this.$http.get('/api/config/getlast100checkins').then(response => {
                this.checkIns = response.body
            })
        }
    },
    created: function() {
        this.loadData()

        setInterval(function() {
            this.loadData()
        }.bind(this), 30000)
    }
})

var addItemsApp = new Vue({
    el: '#add-items',
    data: {

    },
    methods: {
        addClient: function() {
            $("select").material_select()
            manageClientApp.mode = 'create'
            $("#ManageClientModal").modal('open')
        },
        addClientType: function() {
            manageClientTypeApp.mode = 'create'
            $("#ManageClientTypeModal").modal('open')
        },
        addConfig: function() {
            manageConfigApp.mode = 'create'
            $("#ManageConfigModal").modal('open')
        }
    }
})

var manageClientApp = new Vue({
    el: '#ManageClientModal',
    data: {
        client: {
            ClientId: 0,
            Identifier: '',
            ClientType: {
                Name: ''
            }
        },
        clientTypes: [],
        mode: 'create'
    },
    methods: {
        resetClient: function() {
            this.client = {
                ClientId: 0,
                Identifier: '',
                ClientType: {
                    Name: ''
                }
            }
            $("select").material_select()
            $("#clientTypeSelection").val('')
            $("#clientTypeSelection").material_select('')
        },
        addClient: function() {
            loadingApp.loading = true
            this.client.ClientType.Name = $("#clientTypeSelection").val()
            this.$http.post('/api/config/addclient', this.client).then(response => {
                clientApp.loadData()
                loadingApp.loading = false
                this.resetClient()
            })
        },
        updateClient: function() {
            loadingApp.loading = true
            this.client.ClientType.Name = $("#clientTypeSelection").val()
            this.$http.post('/api/config/updateclient', this.client).then(response => {
                clientApp.loadData()
                loadingApp.loading = false
                this.resetClient()
            })
        }
    }
})

var manageClientTypeApp = new Vue({
    el: '#ManageClientTypeModal',
    data: {
        clientType: {
            Name: ''
        },
        mode: 'create'
    },
    methods: {
        resetClientType: function() {
            this.clientType = {
                Name: ''
            }
        },
        addClientType: function() {
            loadingApp.loading = true
            this.$http.post('/api/config/addclienttype', this.clientType).then(response => {
                clientTypeApp.loadData()
                loadingApp.loading = false
                this.resetClientType()
            })
        },
        updateClientType: function() {
            loadingApp.loading = true
            this.$http.post('/api/config/updateclienttype', this.clientType).then(response => {
                clientTypeApp.loadData()
                loadingApp.loading = false
                this.resetClientType()
            })
        }
    }
})

var manageConfigApp = new Vue({
    el: '#ManageConfigModal',
    data: {
        config: {
            Key: '',
            Value: ''
        },
        mode: 'create'
    },
    methods: {
        resetConfig: function() {
            this.config = {
                Key: '',
                Value: ''
            }
        },
        addConfig: function() {
            loadingApp.loading = true
            this.$http.post('/api/config/addconfig', this.config).then(response => {
                configApp.loadData()
                loadingApp.loading = false
                this.resetConfig()
            })
        },
        updateConfig: function() {
            loadingApp.loading = true
            this.$http.post('/api/config/updateconfig/', this.config).then(response => {
                configApp.loadData()
                loadingApp.loading = false
                this.resetConfig()
            })
        }
    }
})

var manageConfigAssociationsApp = new Vue({
    el: '#ManageConfigAssociationsModal',
    data: {
        configAssociations: [],
        config: {},
        client: {},
        clients: [],
        configs: [],
        direction: 'client'
    },
    methods: {
        reset: function() {
            this.configAssociations = []
            this.config = {}
            this.client = {}
        },
        loadAssociations: function() {
            loadingApp.loading = true
            if(this.direction == 'client') {
                this.$http.get('/api/config/getconfigassociationsforclient/' + this.client.clientId).then(response => {
                    var associations = response.body
                    for(var i = 0; i < associations.length; i++) {
                        var configAssociation = {
                            ConfigId: associations[i].configId,
                            ClientId: (this.client.clientId) ? this.client.clientId : 0,
                            Value: associations[i].value
                        }
                        var index = this.configAssociations.length
                        this.$set(this.configAssociations, index, configAssociation)
                        this.initSelect("#configAssociationConfigSelect-" + index, configAssociation.ConfigId)
                    }
                    loadingApp.loading = false
                })
            } else if(this.direction == 'config') {
                this.$http.get('/api/config/getconfigassociationsforconfig/' + this.config.configId).then(response => {
                    var associations = response.body
                    for(var i = 0; i < associations.length; i++) {
                        var configAssociation = {
                            ConfigId: (this.config.configId) ? this.config.configId : 0,
                            ClientId: associations[i].clientId,
                            Value: associations[i].value
                        }
                        var index = this.configAssociations.length
                        this.$set(this.configAssociations, index, configAssociation)
                        this.initSelect("#configAssociationClientSelect-" + index, configAssociation.ClientId)

                    }
                    loadingApp.loading = false
                })
            }
        },
        initSelect: function(elm, val) {
            setTimeout(function() {
                $(elm).val(val)
                $(elm).material_select(val)
            }, 50)
        },
        manageConfigAssociationsForClient: function() {
            loadingApp.loading = true
            this.$http.post('/api/config/manageconfigassociationsforclient/' + this.client.clientId, this.configAssociations).then(response => {
                loadingApp.loading = false
                this.reset()
            })
        },
        manageConfigAssociationsForConfig: function() {
            loadingApp.loading = true
            this.$http.post('/api/config/manageconfigassociationsforconfig/' + this.config.configId, this.configAssociations).then(response => {
                loadingApp.loading = false
                this.reset()
            })
        },
        addConfigAssociation: function() {
            var configAssociation = {
                ConfigId: (this.config.configId) ? this.config.configId : 0,
                ClientId: (this.client.clientId) ? this.client.clientId : 0,
                Value: (this.config.value) ? this.config.value : ''
            }
            this.$set(this.configAssociations, this.configAssociations.length, configAssociation)
            setTimeout(function() {
                $("select").material_select()
            }, 50)
        },
        deleteConfigAssociation: function(index) {
            this.configAssociations.splice(index, 1)
        },
        updateDefaultConfigValue: function(elm) {
            var defaultValue = $(elm).find('option:selected').text()
            var configId = $(elm).val()
            var index = $(elm).attr('id').split('-')[1]
            var configAssociation = this.configAssociations[index]
            configAssociation.Value = defaultValue
            configAssociation.ConfigId = configId
        },
        updateSelectedClient: function(elm) {
            var clientId = $(elm).val()
            var index = $(elm).attr('id').split('-')[1]
            var configAssociation = this.configAssociations[index]
            configAssociation.ClientId = clientId
        }
    }
})