Feature: User
  In order to manage to ad users
  As an api service
  I want to be able to retrieve or create ad users

  Scenario: Update the password for an AAD user
    Given I have an existing user
    And I have an update user request for the new user
    When I send the request to the endpoint
    Then the response should have the status Ok and success status True

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
    Given I have an existing user
    And I have an update user details request for the new user
    When I send the request to the endpoint
    Then the response should have the status Ok and success status True