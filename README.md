# LegendaryTools.AttributeSystem

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Version](https://img.shields.io/badge/version-1.0.0-green.svg)
![Unity](https://img.shields.io/badge/Unity-2020.3%2B-lightgrey.svg)

## Table of Contents

- [Introduction](#introduction)
- [Features](#features)
- [Installation](#installation)
- [Getting Started](#getting-started)
    - [Setup](#setup)
    - [Creating Attributes](#creating-attributes)
    - [Creating Entities](#creating-entities)
    - [Applying Modifiers](#applying-modifiers)
- [Documentation](#documentation)
    - [Core Components](#core-components)
    - [Enums](#enums)
- [Examples](#examples)
    - [Simple Attribute Setup](#simple-attribute-setup)
    - [Applying a Modifier](#applying-a-modifier)
- [Advanced Usage](#advanced-usage)
- [Contributing](#contributing)
- [License](#license)

## Introduction

**LegendaryTools.AttributeSystem** is a robust and flexible attribute management system designed for Unity projects. It allows developers to define and manage complex attributes for game entities, including support for modifiers, conditions, and hierarchical propagation. Whether you're building RPGs, strategy games, or any system requiring intricate attribute interactions, this toolset provides the foundation you need.

## Features

- **Attribute Definitions**: Define attributes with flat values, scaling factors, and more.
- **Modifiers**: Apply modifiers to attributes with support for conditions and propagation rules.
- **Conditions**: Specify conditions under which modifiers are applied, ensuring dynamic and context-sensitive behavior.
- **Hierarchical Propagation**: Control how modifiers propagate through parent-child entity relationships.
- **ScriptableObject Integration**: Easily create and manage configurations using Unity's ScriptableObjects.
- **Editor Enhancements**: (Optional) Integrates with Odin Inspector for enhanced editor support.
- **Entity Management**: Efficiently manage and query entities and their attributes.

## Installation

### - From OpenUPM:

- Open **Edit -> Project Settings -> Package Manager**
- Add a new Scoped Registry (or edit the existing OpenUPM entry)

| Name  | package.openupm.com  |
| ------------ | ------------ |
| URL  | https://package.openupm.com  |
| Scope(s)  | com.legustavinho  |

- Open **Window -> Package Manager**
- Click `+`
- Select `Add package from git URL...`
- Paste `com.legustavinho.legendary-tools-attribute-system` and click `Add`

## Getting Started

### Setup

1. **Create Attribute Configurations**:
    - Navigate to `Assets > Create > Tools > AttributeSystem > AttributeConfig` to create a new Attribute Configuration.
    - Define the attribute's properties, such as whether it has options, capacity, min/max values, and stack penalties.

2. **Create Entity Configurations**:
    - Navigate to `Assets > Create > Tools > AttributeSystem > EntityConfig` to create a new Entity Configuration.
    - Assign tags and define which tags the entity can accept as children.

3. **Initialize the Entity Manager**:
    - Create an empty GameObject in your scene and attach the `EntityManager` script.
    - Assign or create a root entity to manage all other entities.

### Creating Attributes

```csharp
using LegendaryTools.Systems;
using UnityEngine;

public class AttributeSetup : MonoBehaviour
{
    public AttributeConfig strengthConfig;
    public AttributeConfig agilityConfig;

    void Start()
    {
        // Initialize AttributeData
        AttributeData strengthData = AttributeSystemFactory.CreateAttributeData(
            optionsAreFlags: false,
            hasCapacity: true,
            allowExceed: false,
            minCapacity: 0,
            hasMinMax: true,
            minMaxValue: new Vector2(0, 100),
            stackPenaults: new float[] { 0.9f, 0.8f },
            options: null
        );

        // Create AttributeConfig ScriptableObject
        strengthConfig = AttributeSystemFactory.CreateAttributeConfig(strengthData);
        strengthConfig.name = "Strength";

        // Similarly for Agility
        AttributeData agilityData = AttributeSystemFactory.CreateAttributeData(
            optionsAreFlags: false,
            hasCapacity: true,
            allowExceed: false,
            minCapacity: 0,
            hasMinMax: true,
            minMaxValue: new Vector2(0, 100),
            stackPenaults: new float[] { 0.95f, 0.85f },
            options: null
        );

        agilityConfig = AttributeSystemFactory.CreateAttributeConfig(agilityData);
        agilityConfig.name = "Agility";
    }
}
```

### Creating Entities

```csharp
using LegendaryTools.Systems;
using UnityEngine;

public class EntityCreation : MonoBehaviour
{
    public EntityManager entityManager;

    void Start()
    {
        // Create EntityData
        EntityData warriorData = AttributeSystemFactory.CreateEntityData(
            tags: new Tag[] { Tag.Create("Warrior") },
            onlyAcceptTags: new TagFilterMatch[] { TagFilterMatch.Create("Weapon") }
        );

        // Create EntityConfig
        EntityConfig warriorConfig = AttributeSystemFactory.CreateEntityConfig(warriorData, "WarriorConfig");

        // Instantiate Entity
        Entity warrior = new Entity();
        warrior.Initialize(entityManager, warriorConfig);

        // Add Attributes
        Attribute strength = new Attribute(warrior, warriorConfig.Data.attributes.First(a => a.Config.name == "Strength"));
        warrior.AddAttribute(strength);

        Attribute agility = new Attribute(warrior, warriorConfig.Data.attributes.First(a => a.Config.name == "Agility"));
        warrior.AddAttribute(agility);
    }
}
```

### Applying Modifiers

```csharp
using LegendaryTools.Systems;
using UnityEngine;

public class ModifierApplication : MonoBehaviour
{
    public EntityManager entityManager;
    public AttributeConfig strengthConfig;
    public Entity warrior;

    void Start()
    {
        // Create a modifier Attribute
        AttributeData buffData = AttributeSystemFactory.CreateAttributeData(
            optionsAreFlags: false,
            hasCapacity: false,
            allowExceed: false,
            minCapacity: 0,
            hasMinMax: false,
            minMaxValue: Vector2.zero,
            stackPenaults: null,
            options: null
        );

        AttributeConfig buffConfig = AttributeSystemFactory.CreateAttributeConfig(buffData);
        buffConfig.name = "StrengthBuff";

        Attribute strengthBuff = new Attribute(warrior, buffConfig)
        {
            Flat = 10f,
            Factor = 0.2f,
            Type = AttributeType.Modifier,
            Propagation = ModifierPropagation.Parent
        };

        // Define conditions (optional)
        AttributeCondition condition = new AttributeCondition
        {
            Operator = AttributeConditionOperator.AllMustBeTrue
        };
        condition.ModApplicationConditions.Add(new AttributeModifierCondition
        {
            Attribute = strengthConfig,
            Operator = AttributeModOperator.GreaterOrEquals,
            Value = 50f
        });
        strengthBuff.ModifierConditions.Add(condition);

        // Apply the modifier
        bool success = warrior.AddModifier(strengthBuff);
        if (success)
        {
            Debug.Log("Strength buff applied successfully.");
        }
        else
        {
            Debug.LogWarning("Failed to apply strength buff.");
        }
    }
}
```

## Documentation
### Core Components
#### Attribute
Represents an attribute of an entity, such as strength, agility, etc. Attributes can have flat values, scaling factors, and modifiers.

### Properties:

- **Config:** Configuration data for the attribute.
- **Type:** Indicates if it's a base attribute or a modifier.
- **Flat:** Flat value added to the attribute.
- **Factor:** Scaling factor applied to the attribute.
- **Propagation:** How modifiers propagate when applied.
- **ForceApplyIfMissing:** Forces application of modifiers even if the target attribute is missing.
- **CurrentValue:** Current value of the attribute considering capacity.
- **Value:** Computed value after applying all modifiers.
- **Modifiers:** List of modifiers applied to this attribute.

### Methods:

- **AddModifier(Attribute modifier):** Adds a modifier to the attribute.
- **RemoveModifier(Attribute attribute):** Removes a specific modifier.
- **AddUsage(float valueToAdd):** Adds to the attribute's capacity.
- **RemoveUsage(float valueToRemove):** Removes from the attribute's capacity.
- **Clone(IEntity parent):** Creates a clone of the attribute for a new parent entity.

### AttributeCondition

Defines conditions that must be met for a modifier to be applied to an attribute.

- Properties:
    - **Operator:** Determines if all or any conditions must be true.
      -** ModApplicationConditions:** List of specific conditions.

- Methods:
    - **CanBeAppliedOn(IEntity targetEntity):** Checks if conditions are met for a target entity.
      -** Clone():** Creates a copy of the condition.

### AttributeConfig & AttributeData

- **AttributeConfig:** A ScriptableObject that holds AttributeData. Used to define the configuration of an attribute.
- **AttributeData:** Contains all configurable data for an attribute, such as options, capacity, min/max values, and stack penalties.

### Entity & EntityConfig

- **Entity:** Represents an entity in the system, holding a collection of attributes and managing parent-child relationships.
- **EntityConfig:** A ScriptableObject that holds EntityData. Defines the configuration for an entity, including tags and accepted child tags.

### EntityManager

Manages all entities within the system, providing functionalities to add, remove, and query entities.

### Enums

- **AttributeType:** Differentiates between base attributes and modifiers.
- **ModifierPropagation:** Defines how modifiers propagate through entity hierarchies.
- **AttributeConditionOperator:** Specifies whether all or any conditions must be true.
- **AttributeModOperator:** Defines the type of comparison for conditions.
- **AttributeFlagModOperator:** Determines how flags are modified (added, removed, set).
- **AttributeUsageStatus:** Represents the outcome of capacity usage operations.

## Examples

### Simple Attribute Setup

```csharp
using LegendaryTools.Systems;
using UnityEngine;

public class SimpleAttributeExample : MonoBehaviour
{
    public AttributeConfig healthConfig;
    public EntityManager entityManager;

    void Start()
    {
        // Create Health AttributeData
        AttributeData healthData = AttributeSystemFactory.CreateAttributeData(
            optionsAreFlags: false,
            hasCapacity: true,
            allowExceed: false,
            minCapacity: 0,
            hasMinMax: true,
            minMaxValue: new Vector2(0, 100),
            stackPenaults: new float[] { 1.0f },
            options: null
        );

        // Create Health AttributeConfig
        healthConfig = AttributeSystemFactory.CreateAttributeConfig(healthData);
        healthConfig.name = "Health";

        // Create EntityData
        EntityData playerData = AttributeSystemFactory.CreateEntityData(
            tags: new Tag[] { Tag.Create("Player") },
            onlyAcceptTags: new TagFilterMatch[] { TagFilterMatch.Create("Equipment") }
        );

        // Create EntityConfig
        EntityConfig playerConfig = AttributeSystemFactory.CreateEntityConfig(playerData, "PlayerConfig");

        // Instantiate Entity
        Entity player = new Entity();
        player.Initialize(entityManager, playerConfig);

        // Add Health Attribute
        Attribute health = new Attribute(player, healthConfig)
        {
            Flat = 100f,
            Factor = 0f
        };
        player.AddAttribute(health);

        Debug.Log($"Player Health: {health.Value}"); // Outputs: Player Health: 100
    }
}
```

### Applying a Modifier

```csharp
using LegendaryTools.Systems;
using UnityEngine;

public class DamageModifierExample : MonoBehaviour
{
    public AttributeConfig healthConfig;
    public EntityManager entityManager;
    public Entity player;

    void Start()
    {
        // Assume player and healthConfig are already initialized as in the previous example

        // Create a Damage Modifier
        AttributeData damageData = AttributeSystemFactory.CreateAttributeData(
            optionsAreFlags: false,
            hasCapacity: false,
            allowExceed: false,
            minCapacity: 0,
            hasMinMax: false,
            minMaxValue: Vector2.zero,
            stackPenaults: null,
            options: null
        );

        AttributeConfig damageConfig = AttributeSystemFactory.CreateAttributeConfig(damageData);
        damageConfig.name = "DamageModifier";

        Attribute damageModifier = new Attribute(player, damageConfig)
        {
            Flat = -20f, // Reduces health by 20
            Factor = 0f,
            Type = AttributeType.Modifier,
            Propagation = ModifierPropagation.Parent
        };

        // Apply the modifier
        bool success = player.AddModifier(damageModifier);
        if (success)
        {
            Debug.Log($"Player Health after damage: {player.GetAttributeByID(healthConfig).Value}"); // Outputs: Player Health after damage: 80
        }
        else
        {
            Debug.LogWarning("Failed to apply damage modifier.");
        }
    }
}
```

### Advanced Usage

- **Conditional Modifiers:** Define complex conditions under which modifiers are applied, enabling dynamic attribute adjustments based on game states.
- **Flag-Based Attributes:** Utilize bitwise flag operations to manage attribute states efficiently.
- **Hierarchical Entity Structures:** Leverage parent-child relationships to propagate modifiers across entity hierarchies, simplifying attribute management in complex systems.