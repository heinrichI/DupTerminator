using System;
using DupTerminator.WPF.Controls;

namespace DupTerminator.WPF.Abstraction
{
    public interface IThumbnailProvider : IDisposable
    {
        void Enqueue(ImageItemViewModel item);
    }
}