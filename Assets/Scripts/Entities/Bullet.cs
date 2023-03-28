using UnityEngine;

public class Bullet : MonoBehaviour
{
    //  Variables
    //  ---------

    [SerializeField]
    private float m_lifeTime = 0.5f;
    [SerializeField]
    private float m_moveForce = 2000f;

    private float m_shootDate = 0f;
    private Unit m_unitOwner;

    //  Fucntions
    //  ---------

    public void ShootToward(Vector3 direction, Unit owner)
    {
        m_shootDate = Time.time;
        GetComponent<Rigidbody>().AddForce(direction.normalized * m_moveForce);
        m_unitOwner = owner;
    }

    #region MonoBehaviour methods
    void Update()
    {
        if ((Time.time - m_shootDate) > m_lifeTime)
        {
            Destroy(gameObject);
        }
    }
    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.GetComponent<Unit>()?.Team == m_unitOwner.Team)
            return;

        Destroy(gameObject);
    }
    #endregion
}
