Feature: User
  In order to manage to ad users
  As an api service
  I want to be able to retrieve or create ad users

  Scenario: Update the password for an AAD user
    Given I have an existing user
    And I have an update user request for the new user
    When I send the request to the endpoint
    Then the response should have the status Ok and success status True

  Scenario: Update an existing user
    Given I have an existing user
    And I have an update user details request for the new user
    When I send the request to the endpoint
    Then the response should have the status Ok and success status True