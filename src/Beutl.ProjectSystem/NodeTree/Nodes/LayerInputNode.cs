﻿using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

using Beutl.Animation;
using Beutl.Framework;
using Beutl.Media;
using Beutl.NodeTree.Nodes.Group;
using Beutl.Styling;

namespace Beutl.NodeTree.Nodes;

public class LayerInputNode : Node, ISocketsCanBeAdded
{
    public SocketLocation PossibleLocation => SocketLocation.Right;

    public interface ILayerInputSocket : IOutputSocket, IAutomaticallyGeneratedSocket
    {
        void SetProperty(IAbstractProperty property);

        void SetupProperty(CoreProperty property);

        IAbstractProperty? GetProperty();
    }

    public class LayerInputSocket<T> : OutputSocket<T>, ILayerInputSocket, IGroupSocket
    {
        private SetterPropertyImpl<T>? _property;

        static LayerInputSocket()
        {
        }

        public CoreProperty? AssociatedProperty { get; set; }

        public void SetProperty(SetterPropertyImpl<T> property)
        {
            _property = property;
            AssociatedProperty = property.Property;

            property.Setter.Invalidated += OnSetterInvalidated;
        }

        void ILayerInputSocket.SetProperty(IAbstractProperty property)
        {
            SetProperty((SetterPropertyImpl<T>)property);
        }

        IAbstractProperty? ILayerInputSocket.GetProperty()
        {
            return _property;
        }

        public void SetupProperty(CoreProperty property)
        {
            var setter = new Setter<T>((CoreProperty<T>)property);
            SetProperty(new SetterPropertyImpl<T>(setter, property.OwnerType));
        }

        private void OnSetterInvalidated(object? sender, EventArgs e)
        {
            RaiseInvalidated(new RenderInvalidatedEventArgs(this));
        }

        public SetterPropertyImpl<T>? GetProperty()
        {
            return _property;
        }

        public override void PreEvaluate(EvaluationContext context)
        {
            if (GetProperty() is { } property)
            {
                if (property is IAbstractAnimatableProperty<T> { Animation: IAnimation<T> animation })
                {
                    Value = animation.Interpolate(context.Clock.CurrentTime);
                }
                else
                {
                    Value = property.GetValue();
                }
            }
        }

        public override void ReadFromJson(JsonNode json)
        {
            base.ReadFromJson(json);
            string name = (string)json["Property"]!;
            string owner = (string)json["Target"]!;
            Type ownerType = TypeFormat.ToType(owner)!;

            AssociatedProperty = PropertyRegistry.GetRegistered(ownerType)
                .FirstOrDefault(x => x.Name == name);

            if (AssociatedProperty != null)
            {
                SetupProperty(AssociatedProperty);

                GetProperty()?.ReadFromJson(json);
            }
        }

        public override void WriteToJson(ref JsonNode json)
        {
            base.WriteToJson(ref json);
            GetProperty()?.WriteToJson(ref json);
        }
    }

    public bool AddSocket(ISocket socket, [NotNullWhen(true)] out Connection? connection)
    {
        connection = null;
        if (socket is IInputSocket { AssociatedType: { } valueType, Property.Property: { } property } inputSocket)
        {
            Type type = typeof(LayerInputSocket<>).MakeGenericType(valueType);

            if (Activator.CreateInstance(type) is ILayerInputSocket outputSocket)
            {
                ((NodeItem)outputSocket).LocalId = NextLocalId++;
                outputSocket.SetupProperty(property);
                outputSocket.GetProperty()?.SetValue(inputSocket.Property?.GetValue());

                Items.Add(outputSocket);
                if (outputSocket.TryConnect(inputSocket))
                {
                    connection = inputSocket.Connection!;
                    return true;
                }
                else
                {
                    Items.Remove(outputSocket);
                }
            }
        }

        return false;
    }

    public override void ReadFromJson(JsonNode json)
    {
        base.ReadFromJson(json);
        if (json is JsonObject obj)
        {
            if (obj.TryGetPropertyValue("Items", out JsonNode? itemsNode)
                && itemsNode is JsonArray itemsArray)
            {
                int index = 0;
                foreach (JsonObject itemJson in itemsArray.OfType<JsonObject>())
                {
                    if (itemJson.TryGetPropertyValue("@type", out JsonNode? atTypeNode)
                        && atTypeNode is JsonValue atTypeValue
                        && atTypeValue.TryGetValue(out string? atType))
                    {
                        var type = TypeFormat.ToType(atType);
                        ILayerInputSocket? socket = null;

                        if (type?.IsAssignableTo(typeof(ILayerInputSocket)) ?? false)
                        {
                            socket = Activator.CreateInstance(type) as ILayerInputSocket;
                        }

                        if (socket != null)
                        {
                            (socket as IJsonSerializable)?.ReadFromJson(itemJson);
                            Items.Add(socket);
                            ((NodeItem)socket).LocalId = index;
                        }
                    }

                    index++;
                }

                NextLocalId = index;
            }
        }
    }
}
