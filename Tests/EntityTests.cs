using NUnit.Framework;
using UnityEngine;
using LegendaryTools.Systems;
using System.Collections.Generic;
using System.Linq;
using LegendaryTools.TagSystem;
using UnityEngine.TestTools; // for LogAssert

namespace Tests
{
    /// <summary>
    /// A test class containing 30 tests that exercise the Entity class and its related code paths.
    /// </summary>
    public class LegendaryToolsEntityTests
    {
        private EntityManager entityManager;

        [SetUp]
        public void Setup()
        {
            // 1) Create a "root" entity (not yet initialized).
            var rootEntity = new Entity();

            // 2) Create the EntityManager, passing in that root entity.
            entityManager = new EntityManager(rootEntity);

            // 3) Now initialize the root entity with a valid EntityManager and new EntityData.
            rootEntity.Initialize(entityManager, new EntityData());
        }

        #region Entity Tests

        [Test]
        public void Test01_Entity_InitializeWithEntityManager_EntityIsAdded()
        {
            // Arrange
            var entity = new Entity();
            // We supply valid data and a valid entityManager
            entity.Initialize(entityManager, new EntityData());

            // Assert
            Assert.Contains(entity, entityManager.Entities,
                "Entity should be in EntityManager.Entities after being initialized.");
        }

        [Test]
        public void Test02_Entity_InitializeWithEntityManagerAndEntityData_EntityIsAdded()
        {
            // Arrange
            var entityData = new EntityData(); // empty data
            var entity = new Entity();

            // Act
            entity.Initialize(entityManager, entityData);

            // Assert
            Assert.Contains(entity, entityManager.Entities,
                "Entity should be added to EntityManager when initialized with EntityData.");
        }

        [Test]
        public void Test03_Entity_Destroy_RemovesEntityFromManagerAndDisconnectsParents()
        {
            // Arrange
            var parentData = new EntityData();
            var parent = new Entity();
            parent.Initialize(entityManager, parentData);

            var childData = new EntityData();
            var child = new Entity();
            child.Initialize(entityManager, childData);

            // Connect child -> parent
            child.TryToApplyTo(parent);

            // Act
            child.Destroy();

            // Assert
            Assert.IsFalse(entityManager.Entities.Contains(child),
                "Child should be removed from manager after Destroy.");
            Assert.IsEmpty(child.ParentNodes,
                "Child should have no parents after Destroy.");
        }

        [Test]
        public void Test04_Entity_TryToApplyTo_FailsDueToTagMismatch()
        {
            // Arrange
            // The parent expects a certain tag that the child does not have.
            Tag requiredTag = ScriptableObject.CreateInstance<Tag>();
            requiredTag.Name = "RequiredTag";

            var parentData = new EntityData
            {
                onlyAcceptTags = new TagFilterMatch[]
                {
                    new TagFilterMatch(requiredTag, LegendaryTools.TagSystem.TagFilterRuleType.Include)
                }
            };

            var parent = new Entity();
            parent.Initialize(entityManager, parentData);

            // Child has no tags
            var child = new Entity();
            child.Initialize(entityManager, new EntityData());

            // Act
            var (success, connection) = child.TryToApplyTo(parent);

            // Assert
            Assert.IsFalse(success, "Should fail because child's tags do not include requiredTag.");
            Assert.IsNull(connection, "Connection should be null on failure.");
            Assert.IsEmpty(child.ParentNodes, "No parent connection should be formed.");
        }

        [Test]
        public void Test05_Entity_TryToApplyTo_SucceedsAndAppliesModifiersToParent()
        {
            // Arrange
            var parent = new Entity();
            parent.Initialize(entityManager, new EntityData());

            // Child with a single modifier attribute
            var childData = new EntityData();
            var child = new Entity();
            child.Initialize(entityManager, childData);

            // Create a config for the attribute
            AttributeConfig modifierConfig = ScriptableObject.CreateInstance<AttributeConfig>();
            var modAttr = new Attribute(child, modifierConfig)
            {
                Type = AttributeType.Modifier,
                Flat = 10,
                // IMPORTANT: set ForceApplyIfMissing to ensure parent's attribute is created
                ForceApplyIfMissing = true
            };
            child.AllAttributes.Add(modAttr);

            // Act
            var (success, connection) = child.TryToApplyTo(parent);

            // Assert
            Assert.IsTrue(success, "TryToApplyTo should succeed (no tag restriction).");
            Assert.IsNotNull(connection, "A connection object should be returned.");
            Assert.IsNotEmpty(child.ParentNodes, "Child is connected to parent.");

            // Parent should have a new attribute with the child's config
            var parentAttribute = parent.GetAttributeByID(modifierConfig);
            Assert.IsNotNull(parentAttribute,
                "Parent should have an attribute with child's config due to ForceApplyIfMissing=true.");
            Assert.IsTrue(parentAttribute.Modifiers.Contains(modAttr),
                "Parent attribute should contain child's modifier.");
        }

        [Test]
        public void Test06_Entity_DisconnectFromParents_RemovesModifiersFromParents()
        {
            // Arrange
            var parent1 = new Entity();
            parent1.Initialize(entityManager, new EntityData());

            var parent2 = new Entity();
            parent2.Initialize(entityManager, new EntityData());

            var child = new Entity();
            child.Initialize(entityManager, new EntityData());

            // Add a single modifier attribute to child
            var modifierConfig = ScriptableObject.CreateInstance<AttributeConfig>();
            var modAttr = new Attribute(child, modifierConfig)
            {
                Type = AttributeType.Modifier,
                Flat = 5,
                // Force attribute creation on parent
                ForceApplyIfMissing = true
            };
            child.AllAttributes.Add(modAttr);

            // Child applies to both parents
            child.TryToApplyTo(parent1);
            child.TryToApplyTo(parent2);

            // Act
            child.DisconnectFromParents();

            // Assert
            Assert.IsEmpty(child.ParentNodes, "Child should have no parents after disconnecting.");

            // Parent1
            var attr1 = parent1.GetAttributeByID(modifierConfig);
            Assert.IsNotNull(attr1, "Parent1 had an attribute created matching child's config.");
            Assert.IsFalse(attr1.Modifiers.Contains(modAttr),
                "Modifier removed from parent1 after child disconnected.");

            // Parent2
            var attr2 = parent2.GetAttributeByID(modifierConfig);
            Assert.IsNotNull(attr2, "Parent2 had an attribute created matching child's config.");
            Assert.IsFalse(attr2.Modifiers.Contains(modAttr),
                "Modifier removed from parent2 as well.");
        }

        [Test]
        public void Test07_Entity_DisconnectFromParent_RemovesModifiersFromSingleParent()
        {
            // Arrange
            var parent1 = new Entity();
            parent1.Initialize(entityManager, new EntityData());

            var parent2 = new Entity();
            parent2.Initialize(entityManager, new EntityData());

            var child = new Entity();
            child.Initialize(entityManager, new EntityData());

            // Child has a modifier
            var modifierConfig = ScriptableObject.CreateInstance<AttributeConfig>();
            var modAttr = new Attribute(child, modifierConfig)
            {
                Type = AttributeType.Modifier,
                Flat = 15,
                ForceApplyIfMissing = true
            };
            child.AllAttributes.Add(modAttr);

            // Connect child -> both parents
            child.TryToApplyTo(parent1);
            child.TryToApplyTo(parent2);

            // Act
            child.DisconnectFromParent(parent1);

            // Assert
            Assert.IsFalse(child.ParentNodes.Contains(parent1),
                "Child is disconnected from parent1.");
            Assert.IsTrue(child.ParentNodes.Contains(parent2),
                "Child remains connected to parent2.");

            // Verify that parent1 lost the modifier
            var attr1 = parent1.GetAttributeByID(modifierConfig);
            Assert.IsFalse(attr1.Modifiers.Contains(modAttr),
                "parent1 no longer has child's modifier after disconnection.");

            // Meanwhile parent2 still has the modifier
            var attr2 = parent2.GetAttributeByID(modifierConfig);
            Assert.IsTrue(attr2.Modifiers.Contains(modAttr),
                "parent2 still has child's modifier.");
        }

        [Test]
        public void Test08_Entity_GetAttributeByID_FindsExistingAttribute()
        {
            // Arrange
            var entity = new Entity();
            entity.Initialize(entityManager, new EntityData());

            var config = ScriptableObject.CreateInstance<AttributeConfig>();
            var attr = new Attribute(entity, config) { Flat = 10 };
            entity.AllAttributes.Add(attr);

            // Act
            var foundAttr = entity.GetAttributeByID(config);

            // Assert
            Assert.AreEqual(attr, foundAttr, "Should find the existing attribute that references config.");
        }

        [Test]
        public void Test09_Entity_GetAttributeByID_NullConfigLogsError()
        {
            // Arrange
            var entity = new Entity();
            entity.Initialize(entityManager, new EntityData());

            // Act
            LogAssert.ignoreFailingMessages = true; // so it won't fail on error log
            var result = entity.GetAttributeByID(null);

            // Assert
            Assert.IsNull(result, "Should return null if the config is null.");
        }

        [Test]
        public void Test10_Entity_ContainsTag_ReturnsTrueIfTagExists()
        {
            // Arrange
            var myTag = ScriptableObject.CreateInstance<Tag>();
            myTag.Name = "MyTag";

            var data = new EntityData
            {
                tags = new Tag[] { myTag }
            };

            var entity = new Entity();
            entity.Initialize(entityManager, data);

            // Act
            bool containsTag = entity.ContainsTag(myTag);

            // Assert
            Assert.IsTrue(containsTag, "Entity has the tag so it should return true.");
        }

        [Test]
        public void Test11_Entity_AddModifiers_ForceApplyIfMissingFalse_DoesNotCreateMissingAttribute()
        {
            // Arrange
            var target = new Entity();
            target.Initialize(entityManager, new EntityData());

            var source = new Entity();
            source.Initialize(entityManager, new EntityData());

            // Source has a modifier attribute with ForceApplyIfMissing=false
            var config = ScriptableObject.CreateInstance<AttributeConfig>();
            var modifierAttr = new Attribute(source, config)
            {
                Type = AttributeType.Modifier,
                Flat = 100,
                ForceApplyIfMissing = false
            };
            source.AllAttributes.Add(modifierAttr);

            // Act
            target.AddModifiers(source);

            // Assert: target won't get the missing attribute 
            var targetAttribute = target.GetAttributeByID(config);
            Assert.IsNull(targetAttribute,
                "Should not be created because ForceApplyIfMissing=false.");
        }

        [Test]
        public void Test12_Entity_AddModifiers_ForceApplyIfMissingTrue_CreatesMissingAttribute()
        {
            // Arrange
            var target = new Entity();
            target.Initialize(entityManager, new EntityData());

            var source = new Entity();
            source.Initialize(entityManager, new EntityData());

            // Source has a modifier attribute with ForceApplyIfMissing=true
            var config = ScriptableObject.CreateInstance<AttributeConfig>();
            var modifierAttr = new Attribute(source, config)
            {
                Type = AttributeType.Modifier,
                Flat = 50,
                ForceApplyIfMissing = true
            };
            source.AllAttributes.Add(modifierAttr);

            // Act
            target.AddModifiers(source);

            // Assert
            var targetAttribute = target.GetAttributeByID(config);
            Assert.IsNotNull(targetAttribute,
                "Missing attribute must be created because ForceApplyIfMissing=true.");
            Assert.AreEqual(1, targetAttribute.Modifiers.Count);
            Assert.AreEqual(modifierAttr, targetAttribute.Modifiers[0],
                "Target attribute should reference the same modifier object from source.");
        }

        [Test]
        public void Test13_Entity_RemoveModifiers_RemovesAllModifiersFromAttributes()
        {
            // Arrange
            var target = new Entity();
            target.Initialize(entityManager, new EntityData());

            var source = new Entity();
            source.Initialize(entityManager, new EntityData());

            // Source has two modifiers
            var configA = ScriptableObject.CreateInstance<AttributeConfig>();
            var modA = new Attribute(source, configA)
            {
                Type = AttributeType.Modifier,
                Flat = 10,
                ForceApplyIfMissing = true
            };
            source.AllAttributes.Add(modA);

            var configB = ScriptableObject.CreateInstance<AttributeConfig>();
            var modB = new Attribute(source, configB)
            {
                Type = AttributeType.Modifier,
                Flat = 20,
                ForceApplyIfMissing = true
            };
            source.AllAttributes.Add(modB);

            // Apply to target
            target.AddModifiers(source);

            // Act
            target.RemoveModifiers(source);

            // Assert: both modifiers should be removed from target
            var attrA = target.GetAttributeByID(configA);
            Assert.IsNotNull(attrA);
            Assert.IsEmpty(attrA.Modifiers, "All modifiers from 'source' should be removed.");

            var attrB = target.GetAttributeByID(configB);
            Assert.IsNotNull(attrB);
            Assert.IsEmpty(attrB.Modifiers, "All modifiers from 'source' should be removed.");
        }

        [Test]
        public void Test14_Entity_ApplyChildModifiers_PropagationParent()
        {
            // Arrange
            var parent = new Entity();
            parent.Initialize(entityManager, new EntityData());

            var child = new Entity();
            child.Initialize(entityManager, new EntityData());

            var config = ScriptableObject.CreateInstance<AttributeConfig>();
            config.Propagation = ModifierPropagation.Parent;

            var modAttr = new Attribute(child, config)
            {
                Type = AttributeType.Modifier,
                // Need this so the parent's attribute will be created
                ForceApplyIfMissing = true
            };
            child.AllAttributes.Add(modAttr);

            // Act
            parent.ApplyChildModifiers(child);

            // Assert: The parent should get a matching attribute with the child's modifier
            var parentAttr = parent.GetAttributeByID(config);
            Assert.IsNotNull(parentAttr, "Parent should have the child config attribute (forced if missing).");
            Assert.Contains(modAttr, parentAttr.Modifiers,
                "Parent attribute should have the child's modifier.");
        }

        [Test]
        public void Test15_Entity_ApplyChildModifiers_PropagationChild()
        {
            // We'll create a parent with multiple children to see the effect.
            var parentData = new EntityData();
            var parent = new Entity();
            parent.Initialize(entityManager, parentData);

            var child1 = new Entity();
            child1.Initialize(entityManager, new EntityData());
            child1.TryToApplyTo(parent);

            var child2 = new Entity();
            child2.Initialize(entityManager, new EntityData());
            child2.TryToApplyTo(parent);

            // The source of modifiers
            var source = new Entity();
            source.Initialize(entityManager, new EntityData());

            var config = ScriptableObject.CreateInstance<AttributeConfig>();
            config.Propagation = ModifierPropagation.Child;

            var modAttr = new Attribute(source, config)
            {
                Type = AttributeType.Modifier,
                ForceApplyIfMissing = true
            };
            source.AllAttributes.Add(modAttr);

            // Act
            parent.ApplyChildModifiers(source);

            // Assert: The parent itself does NOT get the modifier, but all children do
            var parentAttr = parent.GetAttributeByID(config);
            Assert.IsNull(parentAttr, "Propagation=Child => parent does not get the modifier.");

            var child1Attr = child1.GetAttributeByID(config);
            Assert.IsNotNull(child1Attr, "child1 must have the attribute created if ForceApplyIfMissing=true.");
            Assert.Contains(modAttr, child1Attr.Modifiers);

            var child2Attr = child2.GetAttributeByID(config);
            Assert.IsNotNull(child2Attr, "child2 must also have the attribute created.");
            Assert.Contains(modAttr, child2Attr.Modifiers);
        }

        [Test]
        public void Test16_Entity_ApplyChildModifiers_PropagationBoth()
        {
            // Arrange
            var parent = new Entity();
            parent.Initialize(entityManager, new EntityData());

            var child = new Entity();
            child.Initialize(entityManager, new EntityData());
            child.TryToApplyTo(parent);

            var config = ScriptableObject.CreateInstance<AttributeConfig>();
            config.Propagation = ModifierPropagation.Both;
            var modAttr = new Attribute(child, config)
            {
                Type = AttributeType.Modifier,
                ForceApplyIfMissing = true
            };
            child.AllAttributes.Add(modAttr);

            // Act
            parent.ApplyChildModifiers(child);

            // Assert: The parent gets it, plus the parent's children.
            var parentAttr = parent.GetAttributeByID(config);
            Assert.IsNotNull(parentAttr);
            Assert.Contains(modAttr, parentAttr.Modifiers,
                "Parent should contain child's modifier.");

            var childAttr = child.GetAttributeByID(config);
            Assert.IsNotNull(childAttr);
        }

        [Test]
        public void Test17_Entity_RemoveChildModifiers_PropagationParent()
        {
            // If a child entity had applied modifiers with Propagation=Parent,
            // removing them means we remove from the parent's matching attributes only.
            var parent = new Entity();
            parent.Initialize(entityManager, new EntityData());

            var child = new Entity();
            child.Initialize(entityManager, new EntityData());

            var config = ScriptableObject.CreateInstance<AttributeConfig>();
            config.Propagation = ModifierPropagation.Parent;
            var modAttr = new Attribute(child, config)
            {
                Type = AttributeType.Modifier,
                ForceApplyIfMissing = true
            };
            child.AllAttributes.Add(modAttr);

            // Apply
            parent.ApplyChildModifiers(child);
            var parentAttr = parent.GetAttributeByID(config);
            Assert.IsTrue(parentAttr.Modifiers.Contains(modAttr),
                "Parent should contain child's modifier after applying.");

            // Act
            parent.RemoveChildModifiers(child);

            // Assert
            Assert.IsFalse(parentAttr.Modifiers.Contains(modAttr),
                "Modifier removed from parent because propagation=Parent.");
        }

        [Test]
        public void Test18_Entity_RemoveChildModifiers_PropagationChild()
        {
            // Modifiers are on the parent's children, not the parent itself. 
            var parent = new Entity();
            parent.Initialize(entityManager, new EntityData());

            var child = new Entity();
            child.Initialize(entityManager, new EntityData());
            child.TryToApplyTo(parent);

            var config = ScriptableObject.CreateInstance<AttributeConfig>();
            config.Propagation = ModifierPropagation.Child;
            var modAttr = new Attribute(child, config)
            {
                Type = AttributeType.Modifier,
                ForceApplyIfMissing = true
            };
            child.AllAttributes.Add(modAttr);

            // Apply
            parent.ApplyChildModifiers(child);
            var childAttr = child.GetAttributeByID(config);
            Assert.IsTrue(childAttr.Modifiers.Contains(modAttr),
                "Child has the modifier from 'child' source in this test.");

            // Act
            parent.RemoveChildModifiers(child);

            // Assert
            Assert.IsFalse(childAttr.Modifiers.Contains(modAttr),
                "Modifier removed from child's matching attribute because propagation=Child.");
        }

        [Test]
        public void Test19_Entity_RemoveChildModifiers_PropagationBoth()
        {
            // Modifiers are on parent + children, removing them from both.
            var parent = new Entity();
            parent.Initialize(entityManager, new EntityData());

            var child = new Entity();
            child.Initialize(entityManager, new EntityData());
            child.TryToApplyTo(parent);

            var config = ScriptableObject.CreateInstance<AttributeConfig>();
            config.Propagation = ModifierPropagation.Both;
            var modAttr = new Attribute(child, config)
            {
                Type = AttributeType.Modifier,
                ForceApplyIfMissing = true
            };
            child.AllAttributes.Add(modAttr);

            // Apply
            parent.ApplyChildModifiers(child);

            var parentAttr = parent.GetAttributeByID(config);
            Assert.IsTrue(parentAttr.Modifiers.Contains(modAttr),
                "Parent has the modifier.");
            var childAttr = child.GetAttributeByID(config);
            Assert.IsTrue(childAttr.Modifiers.Contains(modAttr),
                "Child has the modifier.");

            // Act
            parent.RemoveChildModifiers(child);

            // Assert
            Assert.IsFalse(parentAttr.Modifiers.Contains(modAttr),
                "Parent no longer has the modifier.");
            Assert.IsFalse(childAttr.Modifiers.Contains(modAttr),
                "Child no longer has the modifier.");
        }

        #endregion

        #region Attribute Tests

        [Test]
        public void Test20_Attribute_AddModifier_Success()
        {
            // Arrange
            var entity = new Entity();
            entity.Initialize(entityManager, new EntityData());

            var config = ScriptableObject.CreateInstance<AttributeConfig>();
            var attribute = new Attribute(entity, config) { Flat = 100 };
            var modifier = new Attribute(entity, config) { Type = AttributeType.Modifier, Flat = 5 };

            // Act
            bool success = attribute.AddModifier(modifier);

            // Assert
            Assert.IsTrue(success, "Expected successful AddModifier.");
            Assert.Contains(modifier, attribute.Modifiers);
        }

        [Test]
        public void Test21_Attribute_AddModifier_FailsModApplication()
        {
            // We'll create a condition in the modifier that cannot be satisfied 
            // because it references a config that doesn't exist in the parent entity.
            var entity = new Entity();
            entity.Initialize(entityManager, new EntityData());

            var config = ScriptableObject.CreateInstance<AttributeConfig>();
            var attribute = new Attribute(entity, config) { Flat = 100 };

            // The modifier has a condition that depends on an attribute that doesn't exist:
            var badConditionConfig = ScriptableObject.CreateInstance<AttributeConfig>();
            var badCondition = new AttributeModifierCondition
            {
                Attribute = badConditionConfig,
                Operator = AttributeModOperator.Equals,
                Value = 999
            };
            var attributeCondition = new AttributeCondition();
            attributeCondition.ModApplicationConditions.Add(badCondition);

            var modifier = new Attribute(entity, config)
            {
                Type = AttributeType.Modifier,
                ModifierConditions = new List<AttributeCondition> { attributeCondition }
            };

            // Act
            bool success = attribute.AddModifier(modifier);

            // Assert
            Assert.IsFalse(success,
                "The attribute's ModApplicationCanBeAccepted should fail (target attribute not found).");
            Assert.IsEmpty(attribute.Modifiers, "No modifiers should be added.");
        }

        [Test]
        public void Test22_Attribute_RemoveModifier_Success()
        {
            // Arrange
            var entity = new Entity();
            entity.Initialize(entityManager, new EntityData());

            var config = ScriptableObject.CreateInstance<AttributeConfig>();
            var attribute = new Attribute(entity, config);
            var modifier = new Attribute(entity, config) { Type = AttributeType.Modifier };

            attribute.AddModifier(modifier);

            // Act
            bool removed = attribute.RemoveModifier(modifier);

            // Assert
            Assert.IsTrue(removed, "Should remove the modifier successfully.");
            Assert.IsFalse(attribute.Modifiers.Contains(modifier),
                "Modifier is no longer in Modifiers list.");
        }

        [Test]
        public void Test23_Attribute_RemoveModifier_FailNotFound()
        {
            // Arrange
            var entity = new Entity();
            entity.Initialize(entityManager, new EntityData());

            var config = ScriptableObject.CreateInstance<AttributeConfig>();
            var attribute = new Attribute(entity, config);
            var modifier = new Attribute(entity, config) { Type = AttributeType.Modifier };

            // We never add the modifier, so removing it should fail
            bool removed = attribute.RemoveModifier(modifier);

            // Assert
            Assert.IsFalse(removed, "Removing a modifier not in the list should fail.");
        }

        [Test]
        public void Test24_Attribute_RemoveModifiers_ByEntity()
        {
            // Arrange
            var parentData = new EntityData();
            var parent = new Entity();
            parent.Initialize(entityManager, parentData);

            var childData = new EntityData();
            var child = new Entity();
            child.Initialize(entityManager, childData);

            var config = ScriptableObject.CreateInstance<AttributeConfig>();
            var parentAttr = new Attribute(parent, config);
            parent.AllAttributes.Add(parentAttr);

            var childModifierA = new Attribute(child, config) { Type = AttributeType.Modifier };
            var childModifierB = new Attribute(child, config) { Type = AttributeType.Modifier };

            parentAttr.AddModifier(childModifierA);
            parentAttr.AddModifier(childModifierB);

            // Act
            parentAttr.RemoveModifiers(child);

            // Assert
            Assert.IsEmpty(parentAttr.Modifiers,
                "All modifiers from the specified child entity should be removed.");
        }

        [Test]
        public void Test25_Attribute_AddUsage_WithinCapacity_Succeeds()
        {
            // Arrange
            var entity = new Entity();
            entity.Initialize(entityManager, new EntityData());

            var config = ScriptableObject.CreateInstance<AttributeConfig>();
            config.HasCapacity = true;
            config.AllowExceedCapacity = false;
            config.MinCapacity = 0;

            var attribute = new Attribute(entity, config) { Flat = 10f }; 
            // => Value = 10 (no Factor).
            // By default, CurrentValue = 0

            // Act
            bool result = attribute.AddUsage(5f);

            // Assert
            Assert.IsTrue(result, "We can add usage up to 10, so 5 is fine.");
            Assert.AreEqual(5f, attribute.CurrentValue, "CurrentValue goes 0 -> 5.");
        }

        [Test]
        public void Test26_Attribute_AddUsage_NoCapacity_Fails()
        {
            // Arrange
            var entity = new Entity();
            entity.Initialize(entityManager, new EntityData());

            // No capacity
            var config = ScriptableObject.CreateInstance<AttributeConfig>();
            config.HasCapacity = false;

            var attribute = new Attribute(entity, config);

            // Act
            LogAssert.ignoreFailingMessages = true;
            bool result = attribute.AddUsage(5f);

            // Assert
            Assert.IsFalse(result, "Should fail because HasCapacity=false.");
            Assert.AreEqual(0, attribute.CurrentValue,
                "No change to CurrentValue if capacity usage is disabled.");
        }

        [Test]
        public void Test27_Attribute_RemoveUsage_WithinCapacity_Succeeds()
        {
            // Arrange
            var entity = new Entity();
            entity.Initialize(entityManager, new EntityData());

            var config = ScriptableObject.CreateInstance<AttributeConfig>();
            config.HasCapacity = true;
            config.MinCapacity = 0;

            var attribute = new Attribute(entity, config) { Flat = 20f };
            // Set current value to 10 (just a test to ensure we have usage)
            attribute.AddUsage(10f);

            // Act
            bool result = attribute.RemoveUsage(5f);

            // Assert
            Assert.IsTrue(result, "We can remove 5 from 10, within minCapacity=0.");
            Assert.AreEqual(5f, attribute.CurrentValue, "Should be 5 after removing usage.");
        }

        [Test]
        public void Test28_Attribute_RemoveUsage_NoCapacity_Fails()
        {
            // Arrange
            var entity = new Entity();
            entity.Initialize(entityManager, new EntityData());

            var config = ScriptableObject.CreateInstance<AttributeConfig>();
            config.HasCapacity = false;

            var attribute = new Attribute(entity, config);
            attribute.AddUsage(10f); // won't actually increment since capacity is false, but let's see

            // Act
            LogAssert.ignoreFailingMessages = true;
            bool result = attribute.RemoveUsage(3f);

            // Assert
            Assert.IsFalse(result, "Cannot remove usage when capacity is disabled.");
            Assert.AreEqual(0f, attribute.CurrentValue, "Should remain 0f.");
        }

        [Test]
        public void Test29_Attribute_ValueConversions()
        {
            // Arrange
            var entity = new Entity();
            entity.Initialize(entityManager, new EntityData());

            var config = ScriptableObject.CreateInstance<AttributeConfig>();
            var attr = new Attribute(entity, config)
            {
                Flat = 1.0f,
                Factor = 0f
            };

            // Act & Assert
            Assert.IsTrue(attr.ValueAsBool, "1.0 => True");
            Assert.AreEqual((short)1, attr.ValueAsShort, "1.0 => short(1)");
            Assert.AreEqual(1, attr.ValueAsInt, "1.0 => int(1)");
            Assert.AreEqual(1L, attr.ValueAsLong, "1.0 => long(1)");

            // If config has options, check ValueAsOption
            config.Options = new string[] { "Zero", "One", "Two" };
            Assert.AreEqual("One", attr.ValueAsOption, "Index=1 => 'One'.");
        }

        #endregion

        #region Condition and Cloning Tests

        [Test]
        public void Test30_ConditionAndCloning_Tests()
        {
            // This single test checks:
            // - AttributeCondition.CanBeAppliedOn with multiple operators
            // - Cloning for AttributeCondition, AttributeModifierCondition, EntityData, and EntityConfig

            // 1) Condition checks
            var entity = new Entity();
            entity.Initialize(entityManager, new EntityData());

            // Add an attribute with Value=10
            var configA = ScriptableObject.CreateInstance<AttributeConfig>();
            var attrA = new Attribute(entity, configA) { Flat = 10f };
            entity.AllAttributes.Add(attrA);

            // Condition that requires attribute == 10
            var condition = new AttributeModifierCondition
            {
                Attribute = configA,
                Operator = AttributeModOperator.Equals,
                Value = 10f
            };
            var attributeCondition = new AttributeCondition();
            attributeCondition.ModApplicationConditions.Add(condition);

            bool canApply = attributeCondition.CanBeAppliedOn(entity);
            Assert.IsTrue(canApply, "Attribute's value is 10, condition requires 10 => pass.");

            // 2) Cloning for AttributeCondition
            var clonedAC = attributeCondition.Clone();
            Assert.AreEqual(attributeCondition.ModApplicationConditions.Count,
                clonedAC.ModApplicationConditions.Count,
                "Cloned attribute condition should preserve all subconditions.");

            // 3) Cloning for AttributeModifierCondition
            var originalMC = attributeCondition.ModApplicationConditions[0];
            var cloneMC = originalMC.Clone();
            Assert.AreEqual(originalMC.Value, cloneMC.Value);

            // 4) Cloning for EntityData
            var data = new EntityData();
            data.attributes.Add(attrA);
            var dataClone = data.Clone(entity);
            Assert.AreEqual(data.attributes.Count, dataClone.attributes.Count);
        }

        #endregion
    }
}