Feature: User
  In order to manage to ad users
  As an api service
  I want to be able to retrieve or create ad users

  @AddUser
  Scenario: Create a new hearings reforms user account
    Given I have a new hearings reforms user account request with a valid email
    When I send the request to the endpoint
    Then the response should have the status Created and success status True
    And the user should be added

  @AddUser
  Scenario: Create a new hearings reforms test user account
    Given I have a new hearings reforms test user account request with a valid email
    When I send the request to the endpoint
    Then the response should have the status Created and success status True
    And the user should be added

  Scenario: Get user by AD user Id
    Given I have a get user by AD user Id request for an existing user
    When I send the request to the endpoint
    Then the response should have the status OK and success status True
    And the user details should be retrieved

  Scenario: Get user by user principal name
    Given I have a get user by user principal name request for an existing user principal name
    When I send the request to the endpoint
    Then the response should have the status OK and success status True
    And the user details should be retrieved

  Scenario: Delete an AAD user
    Given I have a new user
    And I have a delete user request for the new user
    When I send the request to the endpoint
    Then the response should have the status NoContent and success status True

  Scenario: Update the password for an AAD user
    Given I have a new user
    And I have an update user request for the new user
    When I send the request to the endpoint
    Then the response should have the status Ok and success status True

  Scenario: Get user profile by email
    Given I have a get user profile by email request for an existing email
    When I send the request to the endpoint
    Then the response should have the status OK and success status True
    And the user details should be retrieved

  Scenario: User account created for a user with the same name as existing user
    Given I have a new hearings reforms user account request with an existing name
    When I send the request to the endpoint
    Then the response should have the status Created and success status True
    And the user should be added

  Scenario: Get a list of judges from the AD
    Given I have a valid AD group id and request for a list of judges
    When I send the request to the endpoint
    Then the response should have the status OK and success status True
    And a list of ad judges should be retrieved
    And the list of ad judges should not contain performance test users

  Scenario: Refresh the judges cache
    Given I have a valid refresh judges cache request
    When I send the request to the endpoint
    Then the response should have the status OK and success status True

  Scenario: Update an existing user
    Given I have a new user
    And I have an update user details request for the new user
    When I send the request to the endpoint
    Then the response should have the status Ok and success status True