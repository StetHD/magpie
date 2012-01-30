#include "Node.h"

namespace magpie
{
  temp<BinaryOpNode> BinaryOpNode::create(gc<Node> left, TokenType type,
                                          gc<Node> right)
  {
    return Memory::makeTemp(new BinaryOpNode(left, type, right));
  }
  
  BinaryOpNode::BinaryOpNode(gc<Node> left, TokenType type, gc<Node> right)
  : left_(left),
    type_(type),
    right_(right)
  {}

  void BinaryOpNode::reach()
  {
    Memory::reach(left_);
    Memory::reach(right_);
  }

  void BinaryOpNode::trace(std::ostream& out) const
  {
    out << "(" << left_ << " " << Token::typeString(type_)
        << " " << right_ << ")";
  }
  
  temp<BoolNode> BoolNode::create(bool value)
  {
    return Memory::makeTemp(new BoolNode(value));
  }
  
  BoolNode::BoolNode(bool value)
  : value_(value)
  {}
  
  void BoolNode::trace(std::ostream& out) const
  {
    out << (value_ ? "true" : "false");
  }
  
  temp<IfNode> IfNode::create(gc<Node> condition, gc<Node> thenArm,
                              gc<Node> elseArm)
  {
    return Memory::makeTemp(new IfNode(condition, thenArm, elseArm));
  }
  
  IfNode::IfNode(gc<Node> condition, gc<Node> thenArm, gc<Node> elseArm)
  : condition_(condition),
    thenArm_(thenArm),
    elseArm_(elseArm)
  {}
  
  void IfNode::reach()
  {
    Memory::reach(condition_);
    Memory::reach(thenArm_);
    Memory::reach(elseArm_);
  }
  
  void IfNode::trace(std::ostream& out) const
  {
    out << "(if " << condition_ << " then " << thenArm_;
    
    if (elseArm_.isNull())
    {
      out << ")";
    }
    else
    {
      out << " else " << elseArm_ << ")";
    }
  }
  
  temp<NumberNode> NumberNode::create(double value)
  {
    return Memory::makeTemp(new NumberNode(value));
  }
  
  NumberNode::NumberNode(double value)
  : value_(value)
  {}
  
  void NumberNode::trace(std::ostream& out) const
  {
    out << value_;
  }
}

