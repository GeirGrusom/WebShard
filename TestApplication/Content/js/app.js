var app = angular.module("application", []);

app.controller("testController", function($scope, $http) {
    $scope.users = {
        list: [],
        postUser: { FirstName: '', LastName: '' },
        post: function() {
            $http.post('/', $scope.users.postUser).success(function(value) {
                //$scope.users.list.push(value);
                $scope.users.get();
            });
        },
        get: function() {
            $http.get('/GetAllUsers').success(function (value) {
                $scope.users.list = value;
            });
        }
    }

    $scope.users.get();
});