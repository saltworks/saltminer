using System;

namespace Saltworks.SaltMiner.Core.Entities;

[Serializable]
public class CommentInfo
{

    /// <summary>
    /// Gets or sets ParentId. Parent comment (for discussion/threading)
    /// </summary>
    public string ParentId { get; set; }

    /// <summary>
    /// Gets or sets Message.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Gets or sets User. User that generated this doc or caused this to doc to be generated 
    /// </summary>
    public string User { get; set; }

    /// <summary>
    /// Gets or sets UserFullName. User Full Name that generated this doc or caused this to doc to be generated 
    /// </summary>
    public string UserFullName { get; set; }

    /// <summary>
    /// Gets or sets Type. Type of Comment/Log
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// When the comment was added
    /// </summary>
    public DateTime Added { get; set; }
}