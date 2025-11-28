-- Seed data for tutorial steps and missions
-- Step 1: basic onboarding missions with reward

BEGIN TRANSACTION;

-- Insert tutorial step 1
INSERT INTO TutorialSteps (Id, Code, [Order], TitleKey, IntroKey, RewardMoney, RewardItemTypeId, RewardItemTypeId, RewardItemQuantity)
SELECT
    1 AS Id,
    'TUTORIAL_STEP1' AS Code,
    1 AS [Order],
    'Tutorial.Step1.Title' AS TitleKey,
    'Tutorial.Step1.Intro' AS IntroKey,
    1000 AS RewardMoney,
    it.Id AS RewardItemTypeId,
    0 AS RewardItemType,
    10 AS RewardItemQuantity
FROM ItemTypes it
WHERE it.Code = 'POKE_BALL';

-- Missions for step 1
-- 1. Type /profile
INSERT INTO TutorialMissions (Id, TutorialStepId, [Order], DescriptionKey, ConditionCode)
VALUES (1, 1, 1, 'Tutorial.Step1.M1', 'CMD_PROFILE');

-- 2. Type /move and move to Route 1
INSERT INTO TutorialMissions (Id, TutorialStepId, [Order], DescriptionKey, ConditionCode)
VALUES (2, 1, 2, 'Tutorial.Step1.M2', 'MOVE_ROUTE1');

-- 3. Type /catch and catch a pokemon
INSERT INTO TutorialMissions (Id, TutorialStepId, [Order], DescriptionKey, ConditionCode)
VALUES (3, 1, 3, 'Tutorial.Step1.M3', 'CATCH_ANY_POKEMON');

-- 4. Have 5 pokemon in your pokedex, then type /pokedex
INSERT INTO TutorialMissions (Id, TutorialStepId, [Order], DescriptionKey, ConditionCode)
VALUES (4, 1, 4, 'Tutorial.Step1.M4', 'POKEDEX_5_AND_CMD');

-- 5. Type /map
INSERT INTO TutorialMissions (Id, TutorialStepId, [Order], DescriptionKey, ConditionCode)
VALUES (5, 1, 5, 'Tutorial.Step1.M5', 'CMD_MAP');

-- 6. Type /help
INSERT INTO TutorialMissions (Id, TutorialStepId, [Order], DescriptionKey, ConditionCode)
VALUES (6, 1, 6, 'Tutorial.Step1.M6', 'CMD_HELP');

COMMIT;
